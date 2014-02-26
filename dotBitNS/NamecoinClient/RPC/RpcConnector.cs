﻿// Copyright (c) 2014 George Kimionis
// Distributed under the GPLv3 software license, see the accompanying file LICENSE or http://opensource.org/licenses/GPL-3.0

// Ported to Namecoin by Derrick Slopey Feb 25, 2014

using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using NamecoinLib.ExceptionHandling;
using Newtonsoft.Json;
using dotBitNS;

namespace NamecoinLib.RPC
{

    public sealed class RpcConnector : IRpcConnector
    {
        private readonly String _daemonUrl = ConfigurationManager.AppSettings.Get("DaemonUrl");
        private readonly String _rpcPassword = ConfigurationManager.AppSettings.Get("RpcPassword");
        private readonly String _rpcUsername = ConfigurationManager.AppSettings.Get("RpcUsername");
        private readonly Int16 _rpcRequestTimeoutInSeconds = Int16.Parse(ConfigurationManager.AppSettings.Get("RpcRequestTimeoutInSeconds"));

        public T MakeRequest<T>(RpcMethods rpcMethod, params object[] parameters)
        {
            JsonRpcResponse<T> rpcResponse = MakeRpcRequest<T>(new JsonRpcRequest(1, rpcMethod.ToString(), parameters));
            return rpcResponse.Result;
        }

        private JsonRpcResponse<T> MakeRpcRequest<T>(JsonRpcRequest jsonRpcRequest)
        {
            HttpWebRequest httpWebRequest = MakeHttpRequest(jsonRpcRequest);
            return GetRpcResponse<T>(httpWebRequest);
        }

        private HttpWebRequest MakeHttpRequest(JsonRpcRequest jsonRpcRequest)
        {
            HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(_daemonUrl);
            SetBasicAuthHeader(webRequest, _rpcUsername, _rpcPassword);
            webRequest.Credentials = new NetworkCredential(_rpcUsername, _rpcPassword);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.Timeout = _rpcRequestTimeoutInSeconds * 1000;

            Byte[] byteArray = jsonRpcRequest.GetBytes();
            webRequest.ContentLength = byteArray.Length;

            try
            {
                using (Stream dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }
            }
            catch (Exception exception)
            {
                throw new RpcException("There was a problem sending the request to the namecoin wallet", exception);
            }

            return webRequest;
        }

        private JsonRpcResponse<T> GetRpcResponse<T>(HttpWebRequest httpWebRequest)
        {
            String json = GetJsonResponse(httpWebRequest);

            try
            {
                return JsonConvert.DeserializeObject<JsonRpcResponse<T>>(json);
            }
            catch (JsonException jsonException)
            {
                throw new RpcException("There was a problem deserializing the response from the namecoin wallet", jsonException);
            }
        }

        private static String GetJsonResponse(HttpWebRequest httpWebRequest)
        {
            try
            {
                WebResponse webResponse = httpWebRequest.GetResponse();

                // Deserialize the json response
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        String result = reader.ReadToEnd();
                        reader.Close();
                        return result;
                    }
                }
            }
            catch (ProtocolViolationException protocolViolationException)
            {
                throw new RpcException("Unable to connect to the namecoin server", protocolViolationException);
            }
            catch (WebException webException)
            {
                HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                if (webResponse != null)
                {
                    switch (webResponse.StatusCode)
                    {
                        case HttpStatusCode.InternalServerError:
                            throw new RpcException("The RPC request was either not understood by the Namecoin server or there was a problem executing the request", webException);
                    }
                }
                throw new RpcException("An unknown web exception occurred while trying to read the JSON response", webException);
            }
            catch (Exception exception)
            {
                throw new RpcException("An unknown exception occured while trying to read the JSON response", exception);
            }
        }

        private static void SetBasicAuthHeader(WebRequest webRequest, String username, String password)
        {
            String authInfo = username + ":" + password;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            webRequest.Headers["Authorization"] = "Basic " + authInfo;
        }
    }
}
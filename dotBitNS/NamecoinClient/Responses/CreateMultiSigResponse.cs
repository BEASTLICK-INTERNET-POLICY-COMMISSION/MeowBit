﻿// Copyright (c) 2014 George Kimionis
// Distributed under the GPLv3 software license, see the accompanying file LICENSE or http://opensource.org/licenses/GPL-3.0

using System;

namespace NamecoinLib.Responses
{
    public class CreateMultiSigResponse
    {
        public String Address { get; set; }
        public String RedeemScript { get; set; }
    }
}
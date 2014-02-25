﻿// Copyright (c) 2014 George Kimionis
// Distributed under the GPLv3 software license, see the accompanying file LICENSE or http://opensource.org/licenses/GPL-3.0

using System;

namespace NamecoinLib.Requests.CreateRawTransaction
{
    public class CreateRawTransactionOutput
    {
        public String Address { get; set; }
        public Decimal Amount { get; set; }
    }
}
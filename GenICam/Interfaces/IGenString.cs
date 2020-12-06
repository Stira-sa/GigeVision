﻿using System;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenString
    {
        Task<string> GetValue();

        void SetValue(string value);

        long GetMaxLength();
    }
}
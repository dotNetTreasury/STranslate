﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace STranslate.Model
{
    public interface ITranslator
    {
        Guid Identify { get; set; }

        ServiceType Type { get; set; }

        bool IsEnabled { get; set; }

        IconType Icon { get; set; }

        string Name { get; set; }

        string Url { get; set; }

        TranslationResult Data { get; set; }

        string AppID { get; set; }

        string AppKey { get; set; }

        Task<TranslationResult> TranslateAsync(object request, CancellationToken token);

        Task TranslateAsync(object request, Action<string> OnDataReceived, CancellationToken token);

        ITranslator Clone();
    }
}

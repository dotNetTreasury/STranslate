﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Helper;
using STranslate.Model;
using STranslate.Util;
using STranslate.ViewModels.Preference;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace STranslate.ViewModels
{
    public partial class OutputViewModel : ObservableObject
    {
        [ObservableProperty]
        private BindingList<ITranslator> _translators = Singleton<ServiceViewModel>.Instance.CurTransServiceList;

        [RelayCommand(IncludeCancelCommand = true)]
        private async Task TTS(string text, CancellationToken token)
        {
            await Singleton<TTSViewModel>.Instance.SpeakTextAsync(text, token);
        }

        [RelayCommand]
        private void CopyResult(object obj)
        {
            if (obj is string str && !string.IsNullOrEmpty(str))
            {
                Clipboard.SetDataObject(str);

                ToastHelper.Show("复制成功");
            }
        }

        [RelayCommand]
        private void CopySnakeResult(object obj)
        {
            if (obj is string str && !string.IsNullOrEmpty(str))
            {
                var snakeRet = StringUtil.GenSnakeString(str.Split(' ').ToList());
                Clipboard.SetDataObject(snakeRet);

                ToastHelper.Show("蛇形复制成功");
            }
        }

        [RelayCommand]
        private void CopySmallHumpResult(object obj)
        {
            if (obj is string str && !string.IsNullOrEmpty(str))
            {
                var snakeRet = StringUtil.GenHumpString(str.Split(' ').ToList(), true);
                Clipboard.SetDataObject(snakeRet);

                ToastHelper.Show("小驼峰复制成功");
            }
        }

        [RelayCommand]
        private void CopyLargeHumpResult(object obj)
        {
            if (obj is string str && !string.IsNullOrEmpty(str))
            {
                var snakeRet = StringUtil.GenHumpString(str.Split(' ').ToList(), false);
                Clipboard.SetDataObject(snakeRet);

                ToastHelper.Show("大驼峰复制成功");
            }
        }

        public void Clear()
        {
            foreach (var item in Translators)
            {
                item.Data = TranslationResult.Reset;
            }
        }

        [RelayCommand]
        private void HotkeyCopy(string str)
        {
            if (!int.TryParse(str, out var index))
            {
                return;
            }

            var enabledTranslators = Translators.Where(x => x.IsEnabled).ToList();
            var translator = index == 8 ? enabledTranslators.LastOrDefault() : enabledTranslators.ElementAtOrDefault(index);

            if (translator == null)
            {
                return;
            }

            var result = translator.Data?.Result?.ToString();
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            Clipboard.SetDataObject(result);
            ToastHelper.Show($"复制{translator.Name}结果");
        }
    }
}

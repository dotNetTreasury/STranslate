﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using STranslate.Helper;
using STranslate.Log;
using STranslate.Model;
using STranslate.Util;
using STranslate.ViewModels.Preference.TTS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace STranslate.ViewModels.Preference
{
    public partial class TTSViewModel : ObservableObject
    {
        public async Task SpeakTextAsync(string content, CancellationToken token)
        {
            if (ActivedTTS is null)
            {
                ToastHelper.Show("未启用TTS服务", WindowType.Main);
                return;
            }

            await ActivedTTS.SpeakTextAsync(content, token);
        }

        [ObservableProperty]
        private ITTS? _activedTTS;

        public TTSViewModel()
        {
            //添加默认支持TTS
            //TODO: 新TTS服务需要适配
            TtsServices.Add(new TTSAzure());
            TtsServices.Add(new TTSOffline());

            CurTTSServiceList.OnActiveTTSChanged += CurTTSServiceList_OnActiveTTSChanged;

            ResetView();
        }

        private void CurTTSServiceList_OnActiveTTSChanged(ITTS obj)
        {
            if (obj.IsEnabled)
            {
                ActivedTTS = obj;
                LogService.Logger.Debug($"Change Actived TTS Service: {obj.Identify} {obj.Name}");
            }
        }

        /// <summary>
        /// 重置选中项
        /// </summary>
        /// <param name="type"></param>
        private void ResetView(ActionType type = ActionType.Initialize)
        {
            TtsCounter = CurTTSServiceList.Count;

            //当全部删除时则清空view绑定属性
            if (TtsCounter < 1)
            {
                SelectedIndex = 0;
                TtsServiceContent = null;
                return;
            }

            switch (type)
            {
                case ActionType.Delete:
                    {
                        //不允许小于0
                        SelectedIndex = Math.Max(tmpIndex - 1, 0);
                        TogglePage(CurTTSServiceList[SelectedIndex]);
                        break;
                    }
                case ActionType.Add:
                    {
                        //选中最后一项
                        SelectedIndex = TtsCounter - 1;
                        TogglePage(CurTTSServiceList[SelectedIndex]);
                        break;
                    }
                default:
                    {
                        //初始化默认执行选中第一条
                        SelectedIndex = 0;
                        TogglePage(CurTTSServiceList.First());
                        break;
                    }
            }
        }

        private int tmpIndex;

        [RelayCommand]
        private void TogglePage(ITTS tts)
        {
            if (tts != null)
            {
                if (SelectedIndex != -1)
                    tmpIndex = SelectedIndex;

                string head = "STranslate.Views.Preference.TTS.";
                //TODO: 新TTS服务需要适配
                var name = tts.Type switch
                {
                    TTSType.AzureTTS => string.Format("{0}TTSAzurePage", head),
                    TTSType.OfflineTTS => string.Format("{0}TTSOfflinePage", head),
                    _ => string.Format("{0}TTSAzurePage", head)
                };

                NavigationPage(name, tts);
            }
        }

        [RelayCommand]
        private void Popup(Popup control) => control.IsOpen = true;

        [RelayCommand]
        private void Add(List<object> list)
        {
            if (list?.Count == 2)
            {
                var tts = list.First();

                //TODO: 新TTS服务需要适配
                CurTTSServiceList.Add(tts switch
                {
                    TTSAzure azure => azure.Clone(),
                    TTSOffline offline => offline.Clone(),
                    _ => throw new InvalidOperationException($"Unsupported tts type: {tts.GetType().Name}")
                });

                (list.Last() as Popup)!.IsOpen = false;

                ResetView(ActionType.Add);
            }
        }

        [RelayCommand]
        private void Delete(ITTS tts)
        {
            if (tts != null)
            {
                CurTTSServiceList.Remove(tts);

                ResetView(ActionType.Delete);

                ToastHelper.Show("删除成功", WindowType.Preference);
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (!Singleton<ConfigHelper>.Instance.WriteConfig(CurTTSServiceList))
            {
                LogService.Logger.Debug($"保存TTS失败，{JsonConvert.SerializeObject(CurTTSServiceList)}");

                ToastHelper.Show("保存失败", WindowType.Preference);
            }
            ToastHelper.Show("保存成功", WindowType.Preference);
        }

        [RelayCommand]
        private void Reset()
        {
            CurTTSServiceList = Singleton<ConfigHelper>.Instance.ResetConfig.TTSList ?? [];
            ResetView(ActionType.Initialize);
            ToastHelper.Show("重置配置", WindowType.Preference);
        }

        /// <summary>
        /// 导航页面
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tts"></param>
        public void NavigationPage(string name, ITTS tts)
        {
            UIElement? content = null;

            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("param name is null or empty", nameof(name));

                if (tts == null)
                    throw new ArgumentNullException(nameof(tts));

                Type? type = Type.GetType(name) ?? throw new Exception($"{nameof(NavigationPage)} get {name} exception");

                //读取缓存是否存在，存在则从缓存中获取View实例并通过UpdateVM刷新ViewModel
                if (ContentCache.ContainsKey(type))
                {
                    content = ContentCache[type];
                    if (content is UserControl uc)
                    {
                        var method = type.GetMethod("UpdateVM");
                        method?.Invoke(uc, new[] { tts });
                    }
                }
                else//不存在则创建并通过构造函数传递ViewModel
                {
                    content = (UIElement?)Activator.CreateInstance(type, tts);
                    ContentCache.Add(type, content);
                }

                TtsServiceContent = content;
            }
            catch (Exception ex)
            {
                LogService.Logger.Error("TTS导航出错", ex);
            }
        }

        /// <summary>
        /// 导航 UI 缓存
        /// </summary>
        private readonly Dictionary<Type, UIElement?> ContentCache = [];

        [ObservableProperty]
        private int _selectedIndex = 0;

        [ObservableProperty]
        private UIElement? _ttsServiceContent;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private int _ttsCounter;

        [ObservableProperty]
        private BindingList<ITTS> _ttsServices = [];

        [ObservableProperty]
        private TTSCollection<ITTS> _curTTSServiceList = Singleton<ConfigHelper>.Instance.CurrentConfig?.TTSList ?? [];
    }
}
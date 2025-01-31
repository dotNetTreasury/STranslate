﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Helper;
using STranslate.Log;
using STranslate.Model;
using STranslate.Style.Controls;
using STranslate.Util;
using STranslate.ViewModels.Preference;
using STranslate.Views;
using System;
using System.Collections.Generic;
using System.Windows;

namespace STranslate.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public InputViewModel InputVM { get; } = Singleton<InputViewModel>.Instance;
        public OutputViewModel OutputVM { get; set; } = Singleton<OutputViewModel>.Instance;
        public NotifyIconViewModel NotifyIconVM { get; } = Singleton<NotifyIconViewModel>.Instance;
        public CommonViewModel CommonSettingVM { get; } = Singleton<CommonViewModel>.Instance;

        /// <summary>
        /// 语言字典
        /// </summary>
        private static Dictionary<string, LanguageEnum> Languages => CommonUtil.GetEnumList<LanguageEnum>();

        [ObservableProperty]
        private List<string>? _sourceLanguageList = [.. Languages.Keys];

        [ObservableProperty]
        private List<string>? _targetLanguageList = [.. Languages.Keys];

        [ObservableProperty]
        private string? _selectedSourceLanguage = LanguageEnum.AUTO.GetDescription();

        [ObservableProperty]
        private string? _selectedTargetLanguage = LanguageEnum.AUTO.GetDescription();

        [ObservableProperty]
        private string _isTopMost = ConstStr.TAGFALSE;

        [ObservableProperty]
        private string _isEnableMosehook = ConstStr.TAGFALSE;

        [ObservableProperty]
        private string _topMostContent = ConstStr.UNTOPMOSTCONTENT;

        [ObservableProperty]
        private bool _isOnlyShowRet = false;

        public MainViewModel()
        {
            SqlHelper.InitializeDB();
            // 加载语言选择
            SelectedSourceLanguage = Singleton<ConfigHelper>.Instance.CurrentConfig?.SourceLanguage ?? LanguageEnum.AUTO.GetDescription();
            SelectedTargetLanguage = Singleton<ConfigHelper>.Instance.CurrentConfig?.TargetLanguage ?? LanguageEnum.AUTO.GetDescription();
        }

        [RelayCommand]
        private void Loaded(Window view)
        {
            try
            {
                HotkeyHelper.Hotkeys = Singleton<ConfigHelper>.Instance.CurrentConfig?.Hotkeys ??
                    throw new Exception("快捷键配置出错，请检查配置后重启...");

                NotifyIconVM.OnMousehook += MouseHook;
                NotifyIconVM.OnForbiddenShortcuts += OnForbiddenShortcutsChanged;
                RegisterHotkeys(view);
            }
            catch (Exception ex)
            {
                LogService.Logger.Error($"[Hotkeys] {ex.Message}");
            }
        }

        [RelayCommand]
        private void Closing(Window view)
        {
            NotifyIconVM.OnMousehook -= MouseHook;
            NotifyIconVM.OnForbiddenShortcuts -= OnForbiddenShortcutsChanged;
            UnRegisterHotkeys(view);
        }

        /// <summary>
        /// 禁用/启用快捷键
        /// </summary>
        /// <param name="view"></param>
        /// <param name="forbidden"></param>
        private void OnForbiddenShortcutsChanged(Window view, bool forbidden)
        {
            if (forbidden)
                UnRegisterHotkeys(view);
            else
                RegisterHotkeys(view);
        }

        private void RegisterHotkeys(Window view)
        {
            try
            {
                HotkeyHelper.InitialHook(view);
                HotkeyHelper.Register(HotkeyHelper.InputTranslateId, () =>
                {
                    NotifyIconVM.InputTranslateCommand.Execute(view);
                });

                HotkeyHelper.Register(HotkeyHelper.CrosswordTranslateId, () =>
                {
                    NotifyIconVM.CrossWordTranslateCommand.Execute(view);
                });

                HotkeyHelper.Register(HotkeyHelper.ScreenShotTranslateId, () =>
                {
                    NotifyIconVM.ScreenShotTranslateCommand.Execute(null);
                });

                HotkeyHelper.Register(HotkeyHelper.OpenMainWindowId, () =>
                {
                    NotifyIconVM.OpenMainWindowCommand.Execute(view);
                });

                HotkeyHelper.Register(HotkeyHelper.MousehookTranslateId, () =>
                {
                    NotifyIconVM.MousehookTranslateCommand.Execute(view);
                });

                HotkeyHelper.Register(HotkeyHelper.OCRId, () =>
                {
                    NotifyIconVM.OCRCommand.Execute(null);
                });

                HotkeyHelper.Register(HotkeyHelper.SilentOCRId, () =>
                {
                    NotifyIconVM.SilentOCRCommand.Execute(null);
                });

                HotkeyHelper.Register(HotkeyHelper.ClipboardMonitorId, () =>
                {
                    NotifyIconVM.ClipboardMonitorCommand.Execute(view);
                });

                if (HotkeyHelper.Hotkeys!.InputTranslate.Conflict
                    || HotkeyHelper.Hotkeys!.CrosswordTranslate.Conflict
                    || HotkeyHelper.Hotkeys!.ScreenShotTranslate.Conflict
                    || HotkeyHelper.Hotkeys!.OpenMainWindow.Conflict
                    || HotkeyHelper.Hotkeys!.MousehookTranslate.Conflict
                    || HotkeyHelper.Hotkeys!.OCR.Conflict
                    || HotkeyHelper.Hotkeys!.SilentOCR.Conflict
                    || HotkeyHelper.Hotkeys!.ClipboardMonitor.Conflict)
                {
                    MessageBox_S.Show("全局热键冲突，请前往软件首选项中修改...");
                }
                var msg = "";
                if (!HotkeyHelper.Hotkeys.InputTranslate.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.InputTranslate.Text))
                    msg += $"输入: {HotkeyHelper.Hotkeys.InputTranslate.Text}\n";
                if (!HotkeyHelper.Hotkeys.CrosswordTranslate.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.CrosswordTranslate.Text))
                    msg += $"划词: {HotkeyHelper.Hotkeys.CrosswordTranslate.Text}\n";
                if (!HotkeyHelper.Hotkeys.ScreenShotTranslate.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.ScreenShotTranslate.Text))
                    msg += $"截图: {HotkeyHelper.Hotkeys.ScreenShotTranslate.Text}\n";
                if (!HotkeyHelper.Hotkeys.OpenMainWindow.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.OpenMainWindow.Text))
                    msg += $"显示: {HotkeyHelper.Hotkeys.OpenMainWindow.Text}\n";
                if (!HotkeyHelper.Hotkeys.MousehookTranslate.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.MousehookTranslate.Text))
                    msg += $"鼠标: {HotkeyHelper.Hotkeys.MousehookTranslate.Text}\n";
                if (!HotkeyHelper.Hotkeys.OCR.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.OCR.Text))
                    msg += $"识字: {HotkeyHelper.Hotkeys.OCR.Text}\n";
                if (!HotkeyHelper.Hotkeys.SilentOCR.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.SilentOCR.Text))
                    msg += $"静默: {HotkeyHelper.Hotkeys.SilentOCR.Text}\n";
                if (!HotkeyHelper.Hotkeys.ClipboardMonitor.Conflict && !string.IsNullOrEmpty(HotkeyHelper.Hotkeys.ClipboardMonitor.Text))
                    msg += $"剪贴板: {HotkeyHelper.Hotkeys.ClipboardMonitor.Text}\n";
                NotifyIconVM.UpdateToolTip(msg.TrimEnd('\n'));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void UnRegisterHotkeys(Window view)
        {
            HotkeyHelper.UnRegisterHotKey(view);

            NotifyIconVM.UpdateToolTip($"快捷键禁用");
        }

        [RelayCommand]
        private void LangChanged()
        {
            //清空识别缓存
            InputVM.IdentifyLanguage = string.Empty;
        }

        [RelayCommand]
        private void ExchangeSourceTarget()
        {
            if (SelectedSourceLanguage != SelectedTargetLanguage && !string.IsNullOrEmpty(InputVM.InputContent))
            {
                (SelectedSourceLanguage, SelectedTargetLanguage) = (SelectedTargetLanguage, SelectedSourceLanguage);

                InputVM.TranslateCancelCommand.Execute(null);
                InputVM.TranslateCommand.Execute(null);
            }
        }

        [RelayCommand]
        private void MouseHook(Window view)
        {
            NotifyIconVM.IsMousehook = !NotifyIconVM.IsMousehook;
            IsEnableMosehook = NotifyIconVM.IsMousehook ? ConstStr.TAGTRUE : ConstStr.TAGFALSE;

            if (NotifyIconVM.IsMousehook)
            {
                view.Topmost = true;
                IsTopMost = ConstStr.TAGTRUE;
                TopMostContent = ConstStr.TOPMOSTCONTENT;
                Singleton<MouseHookHelper>.Instance.MouseHookStart();
                Singleton<MouseHookHelper>.Instance.OnGetwordsHandler += OnGetwordsHandlerChanged;
                ToastHelper.Show("启用鼠标划词");
            }
            else
            {
                if (!(Singleton<ConfigHelper>.Instance.CurrentConfig?.IsKeepTopmostAfterMousehook ?? false))
                {
                    view.Topmost = false;
                    IsTopMost = ConstStr.TAGFALSE;
                    TopMostContent = ConstStr.UNTOPMOSTCONTENT;
                }
                Singleton<MouseHookHelper>.Instance.MouseHookStop();
                Singleton<MouseHookHelper>.Instance.OnGetwordsHandler -= OnGetwordsHandlerChanged;
                ToastHelper.Show("关闭鼠标划词");
            }
        }

        private void OnGetwordsHandlerChanged(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;
            InputVM.InputContent = content;

            //如果重复执行先取消上一步操作
            InputVM.TranslateCancelCommand.Execute(null);

            InputVM.TranslateCommand.Execute(null);
        }

        /// <summary>
        /// 点击置顶按钮
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void Sticky(Window win)
        {
            if (NotifyIconVM.IsMousehook)
            {
                MessageBox_S.Show("当前监听鼠标划词中，请先解除监听...");
                return;
            }
            var tmp = !win.Topmost;
            IsTopMost = tmp ? ConstStr.TAGTRUE : ConstStr.TAGFALSE;
            TopMostContent = tmp ? ConstStr.TOPMOSTCONTENT : ConstStr.UNTOPMOSTCONTENT;
            win.Topmost = tmp;

            if (tmp)
            {
                ToastHelper.Show("启用置顶");
            }
            else
            {
                ToastHelper.Show("关闭置顶");
            }
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void Esc(MainView win)
        {
            if (NotifyIconVM.IsMousehook)
            {
                MessageBox_S.Show("当前监听鼠标划词中，请先解除监听...");
                return;
            }

            win.Topmost = false;
            IsTopMost = ConstStr.TAGFALSE;
            TopMostContent = ConstStr.UNTOPMOSTCONTENT;
            win.ViewAnimation(false);
            InputVM.TranslateCancelCommand.Execute(null);

            //取消语音播放
            InputVM.TTSCancelCommand.Execute(null);
            OutputVM.TTSCancelCommand.Execute(null);
        }

        [RelayCommand]
        private void ShowHideInput() => IsOnlyShowRet = !IsOnlyShowRet;

        /// <summary>
        /// 重置字体大小
        /// </summary>
        [RelayCommand]
        private void ResetFontsize() => Application.Current.Resources["FontSize_TextBox"] = 18.0;

        #region 宽、最大高度

        [RelayCommand]
        private void ViewWidthMaxHeightPlus(Window view)
        {
            ViewWidthPlus(view);
            ViewMaxHeightPlus();
        }

        [RelayCommand]
        private void ViewWidthMaxHeightMinus(Window view)
        {
            ViewWidthMinus(view);
            ViewMaxHeightMinus();
        }

        //private double left;
        //private double top;

        [RelayCommand]
        private void ViewWidthPlus(Window view)
        {
            var width = CommonSettingVM.Width.Increment();
            if (width == CommonSettingVM.Width) return;

            //left = view.Left;
            //top = view.Top;
            //if (width == WidthEnum.WorkAreaMaximum)
            //{
            //    //TODO: 优化原始位置缓存，优化多显示器最大化后位置问题
            //    view.Left = 0;
            //    view.Top = 0;
            //}
            CommonSettingVM.Width = width;
        }

        [RelayCommand]
        private void ViewMaxHeightPlus()
        {
            var height = CommonSettingVM.MaxHeight.Increment();
            if (height == CommonSettingVM.MaxHeight) return;

            CommonSettingVM.MaxHeight = height;
        }

        [RelayCommand]
        private void ViewWidthMinus(Window view)
        {
            var width = CommonSettingVM.Width.Decrement();
            if (width == CommonSettingVM.Width) return;

            //view.Left = left;
            //view.Top = top;
            CommonSettingVM.Width = width;
        }

        [RelayCommand]
        private void ViewMaxHeightMinus()
        {
            var height = CommonSettingVM.MaxHeight.Decrement();
            if (height == CommonSettingVM.MaxHeight) return;

            CommonSettingVM.MaxHeight = height;
        }

        [RelayCommand]
        private void ResetMaxHeightWidth(Window view)
        {
            var height = Singleton<ConfigHelper>.Instance.CurrentConfig?.MaxHeight ?? MaxHeight.Maximum;
            var width = Singleton<ConfigHelper>.Instance.CurrentConfig?.Width ?? WidthEnum.Minimum;
            if (height != CommonSettingVM.MaxHeight || width != CommonSettingVM.Width)
            {
                //view.Left = left;
                //view.Top = top;
                CommonSettingVM.Width = width;
                CommonSettingVM.MaxHeight = height;
            }
        }

        #endregion 宽、最大高度

        /// <summary>
        /// 更新主界面图标显示
        /// </summary>
        internal void UpdateMainViewIcons()
        {
            IsShowPreference = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowPreference ?? false;
            IsShowMousehook = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowMousehook ?? false;
            IsShowScreenshot = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowScreenshot ?? false;
            IsShowOCR = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowOCR ?? false;
            IsShowSilentOCR = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowSilentOCR ?? false;
            IsShowClipboardMonitor = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowClipboardMonitor ?? false;
            IsShowQRCode = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowQRCode ?? false;
            IsShowHistory = Singleton<ConfigHelper>.Instance.CurrentConfig?.IsShowHistory ?? false;
        }

        #region 显示图标

        [ObservableProperty]
        private bool isShowPreference;

        [ObservableProperty]
        private bool isShowMousehook;

        [ObservableProperty]
        private bool isShowScreenshot;

        [ObservableProperty]
        private bool isShowOCR;

        [ObservableProperty]
        private bool isShowSilentOCR;

        [ObservableProperty]
        private bool isShowClipboardMonitor;

        [ObservableProperty]
        private bool isShowQRCode;

        [ObservableProperty]
        private bool isShowHistory;

        #endregion 显示图标
    }
}
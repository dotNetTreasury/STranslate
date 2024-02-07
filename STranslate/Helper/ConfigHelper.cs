﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STranslate.Log;
using STranslate.Model;
using STranslate.Util;
using STranslate.ViewModels;
using STranslate.ViewModels.Preference;
using STranslate.ViewModels.Preference.Services;

namespace STranslate.Helper;

public class ConfigHelper
{
    #region 公共方法

    public ConfigHelper()
    {
        if (!Directory.Exists(ApplicationData)) //判断是否存在
        {
            Directory.CreateDirectory(ApplicationData); //创建新路径
            ShortcutUtil.SetDesktopShortcut(); //创建桌面快捷方式
        }
        if (!File.Exists(CnfName)) //文件不存在
        {
            FileStream fs = new(CnfName, FileMode.Create, FileAccess.ReadWrite);
            fs.Close();
            WriteConfig(InitialConfig());
        }

        //初始化时将初始值赋给Config属性
        CurrentConfig = ResetConfig;
    }

    /// <summary>
    /// 初始化操作
    /// </summary>
    public void InitialOperate()
    {
        //初始化主题
        ThemeOperate(CurrentConfig?.ThemeType ?? ThemeType.Light);

        //初始化字体
        FontOperate();

        //初始化代理设置
        ProxyUtil.UpdateDynamicProxy(CurrentConfig?.IsDisableSystemProxy ?? false);

        //初始化首页图标
        Singleton<MainViewModel>.Instance.UpdateMainViewIcons();
    }

    /// <summary>
    /// 退出时保存位置
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool WriteConfig(double x, double y)
    {
        bool isSuccess = false;
        if (CurrentConfig is not null)
        {
            CurrentConfig.Position = $"{x},{y}";
            WriteConfig(CurrentConfig);
            isSuccess = true;
        }
        return isSuccess;
    }

    /// <summary>
    /// 写入服务到配置
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public bool WriteConfig(BindingList<ITranslator> services)
    {
        var isSuccess = false;
        if (CurrentConfig is null) return isSuccess;
        CurrentConfig.Services = services;
        WriteConfig(CurrentConfig);
        isSuccess = true;
        return isSuccess;
    }

    /// <summary>
    /// 写入源语言、目标语言到配置
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool WriteConfig(string source, string target)
    {
        var isSuccess = false;
        if (CurrentConfig is null) return isSuccess;
        CurrentConfig.SourceLanguage = source;
        CurrentConfig.TargetLanguage = target;
        WriteConfig(CurrentConfig);
        isSuccess = true;
        return isSuccess;
    }

    /// <summary>
    /// 写入热键到配置
    /// </summary>
    /// <param name="hotkeys"></param>
    /// <returns></returns>
    public bool WriteConfig(Hotkeys hotkeys)
    {
        var isSuccess = false;
        if (CurrentConfig is null) return isSuccess;
        CurrentConfig.Hotkeys = hotkeys;
        WriteConfig(CurrentConfig);
        isSuccess = true;
        return isSuccess;
    }

    /// <summary>
    /// 写入常规配置项到当前配置
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool WriteConfig(CommonViewModel model)
    {
        var isSuccess = false;
        if (CurrentConfig is null) return isSuccess;
        CurrentConfig.IsStartup = model.IsStartup;
        CurrentConfig.NeedAdministrator = model.NeedAdmin;
        CurrentConfig.HistorySize = model.HistorySize;
        CurrentConfig.AutoScale = model.AutoScale;
        CurrentConfig.ThemeType = model.ThemeType;
        CurrentConfig.IsFollowMouse = model.IsFollowMouse;
        CurrentConfig.CloseUIOcrRetTranslate = model.CloseUIOcrRetTranslate;
        CurrentConfig.UnconventionalScreen = model.UnconventionalScreen;
        CurrentConfig.IsDisableSystemProxy = model.IsDisableSystemProxy;
        CurrentConfig.IsOcrAutoCopyText = model.IsOcrAutoCopyText;
        CurrentConfig.IsAdjustContentTranslate = model.IsAdjustContentTranslate;
        CurrentConfig.IsRemoveLineBreakGettingWords = model.IsRemoveLineBreakGettingWords;
        CurrentConfig.DoubleTapTrayFunc = model.DoubleTapTrayFunc;
        CurrentConfig.CustomFont = model.CustomFont;
        CurrentConfig.IsKeepTopmostAfterMousehook = model.IsKeepTopmostAfterMousehook;
        CurrentConfig.IsShowPreference = model.IsShowPreference;
        CurrentConfig.IsShowMousehook = model.IsShowMousehook;
        CurrentConfig.IsShowScreenshot = model.IsShowScreenshot;
        CurrentConfig.IsShowOCR = model.IsShowOCR;
        CurrentConfig.IsShowSilentOCR = model.IsShowSilentOCR;
        CurrentConfig.IsShowQRCode = model.IsShowQRCode;
        CurrentConfig.IsShowHistory = model.IsShowHistory;
        CurrentConfig.WordPickingInterval = model.WordPickingInterval;
        CurrentConfig.IsHideOnStart = model.IsHideOnStart;
        CurrentConfig.ShowCopyOnHeader = model.ShowCopyOnHeader;
        CurrentConfig.MaxHeight = model.MaxHeight;
        Singleton<MainViewModel>.Instance.UpdateMainViewIcons();
        ThemeOperate(CurrentConfig.ThemeType);

        WriteConfig(CurrentConfig);
        isSuccess = true;
        return isSuccess;
    }

    private void ThemeOperate(ThemeType themeType)
    {
        switch (themeType)
        {
            case ThemeType.Auto:
                Singleton<ThemeHelper>.Instance.StartListenRegistry();
                break;
            case ThemeType.Light:
                Singleton<ThemeHelper>.Instance.LightTheme();
                goto default;
            case ThemeType.Dark:
                Singleton<ThemeHelper>.Instance.DarkTheme();
                goto default;
            default:
                Singleton<ThemeHelper>.Instance.StopListenRegistry();
                break;
        }
    }

    //初始化字体
    private void FontOperate()
    {
        try
        {
            Application.Current.Resources[ConstStr.USERDEFINEFONTKEY] = CurrentConfig!.CustomFont.Equals(ConstStr.DEFAULTFONTNAME)
                ? Application.Current.Resources[ConstStr.DEFAULTFONTNAME] : new FontFamily(CurrentConfig!.CustomFont);
        }
        catch (Exception)
        {
            Application.Current.Resources[ConstStr.USERDEFINEFONTKEY] = Application.Current.Resources[ConstStr.DEFAULTFONTNAME];
            CurrentConfig!.CustomFont = ConstStr.DEFAULTFONTNAME;
        }
    }

    #endregion 公共方法

    #region 私有方法

    private ConfigModel ReadConfig()
    {
        try
        {
            var settings = new JsonSerializerSettings { Converters = { new TranslatorConverter() } };
            var content = File.ReadAllText(CnfName);
            var config = JsonConvert.DeserializeObject<ConfigModel>(content, settings) ?? throw new Exception("反序列化失败...");
            // 读取时解密AppID、AppKey
            config.Services?.ToList().ForEach(service =>
            {
                service.AppID = string.IsNullOrEmpty(service.AppID) ? service.AppID : DESUtil.DesDecrypt(service.AppID);
                service.AppKey = string.IsNullOrEmpty(service.AppKey) ? service.AppKey : DESUtil.DesDecrypt(service.AppKey);
            });
            return config;
        }
        catch (Exception ex)
        {
            // 备份当前config
            var path = BackupCurrentConfig();

            LogService.Logger.Error($"[READ CONFIG] 读取配置错误，已备份旧配置至: {path} 当前加载初始化配置...", ex);
            return InitialConfig();
        }
    }

    /// <summary>
    /// 备份当前配置文件
    /// </summary>
    private string BackupCurrentConfig()
    {
        var backupFilePath = $"{ApplicationData}\\{_appName.ToLower()}_{DateTime.Now:yyyyMMdd_HHmmssfff}.json";
        File.Copy(CnfName, backupFilePath, true );
        return backupFilePath;
    }

    private void WriteConfig(ConfigModel conf)
    {
        var copy = conf.ServiceDeepClone();
        // 写入时加密AppID、AppKey
        copy.Services?.ToList().ForEach(service =>
        {
            service.AppID = string.IsNullOrEmpty(service.AppID) ? service.AppID : DESUtil.DesEncrypt(service.AppID);
            service.AppKey = string.IsNullOrEmpty(service.AppKey) ? service.AppKey : DESUtil.DesEncrypt(service.AppKey);
        });
        File.WriteAllText(CnfName, JsonConvert.SerializeObject(copy, Formatting.Indented));
    }

    #endregion 私有方法

    #region 字段 && 属性

    /// <summary>
    /// 重置Config
    /// </summary>
    public ConfigModel ResetConfig => ReadConfig();

    /// <summary>
    /// 初始Config
    /// </summary>
    private ConfigModel InitialConfig()
    {
        var hk = new Hotkeys();
        hk.InputTranslate.Update(KeyModifiers.MOD_ALT, KeyCodes.A, ConstStr.DEFAULTINPUTHOTKEY);
        hk.CrosswordTranslate.Update(KeyModifiers.MOD_ALT, KeyCodes.D, ConstStr.DEFAULTCROSSWORDHOTKEY);
        hk.ScreenShotTranslate.Update(KeyModifiers.MOD_ALT, KeyCodes.S, ConstStr.DEFAULTSCREENSHOTHOTKEY);
        hk.OpenMainWindow.Update(KeyModifiers.MOD_ALT, KeyCodes.G, ConstStr.DEFAULTOPENHOTKEY);
        hk.MousehookTranslate.Update(KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, KeyCodes.D, ConstStr.DEFAULTMOUSEHOOKHOTKEY);
        hk.OCR.Update(KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, KeyCodes.S, ConstStr.DEFAULTOCRHOTKEY);
        hk.SilentOCR.Update(KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, KeyCodes.F, ConstStr.DEFAULTSILENTOCRHOTKEY);
        return new ConfigModel
        {
            HistorySize = 100,
            AutoScale = 0.8,
            Hotkeys = hk,
            ThemeType = ThemeType.Light,
            IsStartup = false,
            IsFollowMouse = false,
            IsOcrAutoCopyText = false,
            UnconventionalScreen = false,
            IsDisableSystemProxy = false,
            CloseUIOcrRetTranslate = false,
            IsAdjustContentTranslate = false,
            IsRemoveLineBreakGettingWords = false,
            DoubleTapTrayFunc = DoubleTapFuncEnum.InputFunc,
            CustomFont = ConstStr.DEFAULTFONTNAME,
            IsKeepTopmostAfterMousehook = false,
            IsShowPreference = false,
            IsShowMousehook = false,
            IsShowScreenshot = false,
            IsShowOCR = false,
            IsShowSilentOCR = false,
            IsShowQRCode = false,
            IsShowHistory = false,
            WordPickingInterval = 100,
            IsHideOnStart = false,
            ShowCopyOnHeader = false,
            MaxHeight = MaxHeight.Maximum,
            SourceLanguage = LanguageEnum.AUTO.GetDescription(),
            TargetLanguage = LanguageEnum.AUTO.GetDescription(),
            Services =
            [
                new TranslatorSTranslate(Guid.NewGuid(), "", "STranslate", IconType.STranslate),
                new TranslatorApi(Guid.NewGuid(), "https://googlet.deno.dev/translate", "Google", IconType.Google),
                new TranslatorApi(Guid.NewGuid(), "https://deeplx.deno.dev/translate", "DeepL", IconType.DeepL),
                new TranslatorApi(Guid.NewGuid(), "https://iciba.deno.dev/translate", "爱词霸", IconType.Iciba, isEnabled: false)
            ]
        };
    }

    public ConfigModel? CurrentConfig { get; private set; }

    /// <summary>
    /// 配置文件
    /// </summary>
    private string CnfName => $"{ApplicationData}\\{_appName.ToLower()}.json";

    /// <summary>
    /// C:\Users\user\AppData\Local\STranslate
    /// </summary>
    private string ApplicationData => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{_appName}";


    private readonly string _appName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);

    #endregion 字段 && 属性
}

#region JsonConvert

public class TranslatorConverter : JsonConverter<ITranslator>
{
    public override ITranslator ReadJson(
        JsonReader reader,
        Type objectType,
        ITranslator? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        JObject jsonObject = JObject.Load(reader);

        // 根据Type字段的值来决定具体实现类
        var type = jsonObject["Type"]!.Value<int>();

        ITranslator translator = type switch
        {
            (int)ServiceType.STranslateService => new TranslatorSTranslate(),
            (int)ServiceType.ApiService => new TranslatorApi(),
            (int)ServiceType.BaiduService => new TranslatorBaidu(),
            (int)ServiceType.MicrosoftService => new TranslatorMicrosoft(),
            (int)ServiceType.OpenAIService => new TranslatorOpenAI(),
            (int)ServiceType.GeminiService => new TranslatorGemini(),
            (int)ServiceType.TencentService => new TranslatorTencent(),
            (int)ServiceType.AliService => new TranslatorAli(),
            (int)ServiceType.YoudaoService => new TranslatorYoudao(),
            (int)ServiceType.NiutransService => new TranslatorNiutrans(),
            (int)ServiceType.CaiyunService => new TranslatorCaiyun(),
            (int)ServiceType.VolcengineService => new TranslatorVolcengine(),
            (int)ServiceType.EcdictService => new TranslatorEcdict(),
            //TODO: 新接口需要适配
            _ => throw new NotSupportedException($"Unsupported ServiceType: {type}")
        };

        serializer.Populate(jsonObject.CreateReader(), translator);
        return translator;
    }

    public override void WriteJson(JsonWriter writer, ITranslator? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public static class ObjectExtensions
{
    public static T ServiceDeepClone<T>(this T source)
    {
        if (source == null)
            return default!;

        var json = JsonConvert.SerializeObject(source);
        var settings = new JsonSerializerSettings { Converters = { new TranslatorConverter() } };
        return JsonConvert.DeserializeObject<T>(json, settings)!;
    }
}

#endregion JsonConvert
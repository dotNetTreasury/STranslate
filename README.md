
WPF 开发的一款即用即走的翻译工具

## 功能

- [x] 添加 DeepL API
- [x] 添加百度翻译 API [申请教程（Bob）](https://bobtranslate.com/service/translate/baidu.html)
- [x] 实现基本翻译功能
- [ ] 自动生成配置文件
- [ ] 优化 DeepL 服务并打包成库引入进项目
- [ ] 添加缓存功能
- [ ] 添加划词翻译
- [ ] 添加 OCR 翻译
- [ ] 设置 UI 化
- [ ] 使用说明 UI 化
- [ ] 软件层面识别语种（UI 提示识别语种）
- [ ] 翻译制作成插件方式
- [ ] 优化软件发布方式(自动升级)

## 使用

打开软件后会静默在后台，等待调用，全局监听快捷键（日后升级成自定义）`Alt` + `A` 即可打开软件主界面，输入需要翻译的内容，选择目标语言，如果识别语种不正确则手动指定即可，翻译结束后选择复制或点击一键复制即可

点击软件外部界面任意处或点击ESC，软件则会自动隐藏到后台，即用即走。

> 当前不能生成配置文件，需要手动再 D 盘根目录创建一个名为 `STranslate.yml` 的配置文件，内容格式如下

```yml
service: baidu
baidu:
    appid: 
    secretKey: 
deepl:
    url:
```
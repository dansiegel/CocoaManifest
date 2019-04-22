# CocoaManifest

This is a Proof of Concept Repo for [xamarin-macios issue 5916](https://github.com/xamarin/xamarin-macios/issues/5916).

The intent of this is to make it easier to configure your Entitlements and Info.plist in code. This provides several advantages. It can be easier to see this in code than in the Xamarin Plist Editors. Additionally because you already need to deal with secrets in code this helps to reduce additional build steps by allowing you to swap in the correct values in code for secrets such as your Client Id when using the Microsoft Identity Client, or the App Secret from App Center when using App Center Distribution.

```cs
[assembly: BundleUrlType(Name = "com.avantipoint.foo", Role = BundleUrlTypeRole.Editor, Schemes = new[] { "msal0000-0000-00000000" })]

[assembly: UsesBackgroundMode(InfoPlist.BackgroundMode.BackgroundFetch)]
[assembly: UsesBackgroundMode(InfoPlist.BackgroundMode.RemoteNotifications)]

[assembly: UsesCapability(InfoPlist.Capabilities.StillCamera)]
[assembly: Privacy(InfoPlist.PrivacyUsage.Camera, "We want beautiful pictures of you")]
```
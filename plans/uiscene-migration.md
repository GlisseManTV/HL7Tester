# Migration UIScene — Solution finale (Mac Catalyst)

## Contexte

La migration UIScene requise pour le lancement sous Xcode 27 / macOS 27 est appliquée et vérifiée pour Mac Catalyst. L'application démarre après correction du nom de configuration de scène attendu par MAUI.

Références :
- https://github.com/dotnet/maui/pull/38601
- https://developer.apple.com/documentation/technotes/tn3187-migrating-to-the-uikit-scene-based-life-cycle

---

## Implémentation retenue

### 1. Delegate de scène Mac Catalyst

Le delegate hérite de `MauiUISceneDelegate`, qui prend en charge l'initialisation MAUI de la scène :

```csharp
using Foundation;
using UIKit;

namespace HL7Tester;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
```

> `MauiUISceneDelegate` est fourni par le SDK MAUI .NET 10 (PR #38601).
> Il gère automatiquement la création de la `UIWindow` et l'initialisation de l'app MAUI.

---

### 2. Configuration Mac Catalyst

`Platforms/MacCatalyst/Info.plist` déclare `UIApplicationSceneManifest` avec le delegate `SceneDelegate`. Le nom doit être exactement `__MAUI_DEFAULT_SCENE_CONFIGURATION__` : `Default Configuration` provoquait le message UIKit indiquant que la configuration recherchée par MAUI était absente, suivi d'un fallback.

```xml
<key>UIApplicationSceneManifest</key>
<dict>
    <key>UIApplicationSupportsMultipleScenes</key>
    <false/>
    <key>UISceneConfigurations</key>
    <dict>
        <key>UIWindowSceneSessionRoleApplication</key>
        <array>
            <dict>
                <key>UISceneConfigurationName</key>
                <string>__MAUI_DEFAULT_SCENE_CONFIGURATION__</string>
                <key>UISceneDelegateClassName</key>
                <string>SceneDelegate</string>
            </dict>
        </array>
    </dict>
</dict>
```

Le manifeste permet une seule scène (`UIApplicationSupportsMultipleScenes = false`) et associe le rôle `UIWindowSceneSessionRoleApplication` à `SceneDelegate`.

---

### 3. AppDelegate et point d'entrée

Les fichiers [`Platforms/MacCatalyst/AppDelegate.cs`](V2/HL7Tester/Platforms/MacCatalyst/AppDelegate.cs) et [`Platforms/iOS/AppDelegate.cs`](V2/HL7Tester/Platforms/iOS/AppDelegate.cs) **restent inchangés**.

Le `MauiUIApplicationDelegate` du SDK .NET 10 gère désormais la délégation vers le `SceneDelegate` automatiquement.

```csharp
// PAS DE CHANGEMENT — reste tel quel
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

---

### 4. Portée

Les fichiers [`Platforms/MacCatalyst/Program.cs`](V2/HL7Tester/Platforms/MacCatalyst/Program.cs) et [`Platforms/iOS/Program.cs`](V2/HL7Tester/Platforms/iOS/Program.cs) **restent inchangés**.

`UIApplication.Main(args, null, typeof(AppDelegate))` est toujours le point d'entrée correct avec UIScene.

---

Cette application ne cible pas iOS dans le périmètre demandé. Le plist iOS et le delegate iOS n'ont donc pas été modifiés dans le correctif final.

---

## Validation finale

La cible `net10.0-maccatalyst` a été construite avec succès (0 avertissement, 0 erreur), puis l'application a été relancée et l'utilisateur a confirmé qu'elle démarre. Aucun delegate UIKit manuel ni changement à `AppDelegate.cs` ou `Program.cs` n'a été nécessaire. Windows n'est pas concerné.

# Liminal-SDK

### Programs
Unity 2018.3.8f1

### Importing the _Liminal SDK_

The _Liminal SDK_ provides its own copies of the Oculus VR & Gear VR SDKs. Before importing the _Liminal SDK_, check that these SDKs are not already included in your project:
> * OVR (Oculus)
> * GVR (Google VR)
> * Liminal

If you have any of the SDKs above: 
> 1. Close Unity
> 2. Delete the SDK files from your project via your file browser. 

**Important:** You must close Unity, otherwise the SDKs will auto-generate again.

It is now safe to import the _Liminal SDK_. 
The Liminal SDK use Unity Package Manager.
> 1. Clone LiminalSDK from `https://github.com/LiminalVR/LiminalPlatformSDK.git` to the same directory as your Application
> 3. Locate YourProjectName/Packages/manifest.json
> 4. Include Liminal SDK by adding the package name and path to where you cloned it. Add `"com.liminal.sdk": "file:../../LiminalPlatformSDK/Unity/Liminal.SDK/Assets/Liminal",` (at the top of the .json file)
> 5. Open the Unity Project and the Liminal SDK will automatically be imported.
> 6. Open Windows > Unity Package Manager and import Oculus (Android)

#### Updating the SDK
> Liminal > Update Package

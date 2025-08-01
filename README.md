[![Sponsor](https://img.shields.io/badge/Sponsor-❤️_GitHub_Sponsors-orange?logo=github&style=for-the-badge)](https://github.com/sponsors/versile2)

# Blazor Lottie Player

Blazor Lottie Player makes it easy to integrate high-quality Lottie animations into your Blazor applications.

## 🚀 Getting Started

### 1. Install the NuGet Package

Add [Blazor.Lottie.Player](https://mudx.org/docs/lottie) to your project via NuGet.

### 2. Choose How to Use the Component

There are two ways to use Blazor Lottie Player:

#### a. Using the Built-in Component

- Add `@using Blazor.Lottie.Player` to your `_Imports.razor` file.  
- Use the `<LottiePlayer>` component in your Razor files, setting the `Src` parameter to your Lottie animation JSON file.  
- The player will size itself to fill its parent container. Use CSS to control its width and height as needed.

#### b. Using the Module Programmatically (Advanced)

- Add `@using Blazor.Lottie.Player` to your `_Imports.razor` file.  
- Include the Lottie Web library (The LottiePlayer component loads it from the below CDN):

  ```html
  <script src="https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.13.0/lottie.min.js"></script>
  ```
 
- Instantiate and use the `LottiePlayer` class in your code-behind or component logic:

  ```csharp
  var player = new LottiePlayerModule(IJSRuntime, ElementReference);
  await player.InitializeAsync(LottiePlaybackOptions);
  // Optional Commands
  await player.PlayAsync();
  await player.PauseAsync();
  await player.StopAsync();
  await player.GoToAndPlay(double frame, true);
  await player.GoToAndStop(double frame, true);
  await player.SetSpeedAsync(double speed, true);
  await player.SetDirectionAsync(LottieDirection.Forward);
  // Subscribe to Events
  player.OnAnimationLoaded += (args) => { /* Handle Animation Loaded */ };
  player.OnDOMLoaded += (args) => { /* Handle DOM Loaded */ };
  player.OnAnimationComplete += () => { /* Handle Animation Complete */ };
  player.OnEnterFrame += (args) => { /* Handle Enter Frame */ };
  player.OnComplete += () => { /* Handle Complete */ };
  player.OnLoopComplete += () => { /* Handle Loop Complete */ };
  ```

### 3. Read the Docs

Visit the [Blazor Lottie Player documentation](https://mudx.org/docs/lottie) for detailed usage instructions, examples, and API reference.

### 4. Get Help, Support, or Contribute

If you need help, want to report an issue, or would like to support or contribute to the project, please visit the [GitHub repository](https://github.com/MudXtra/LottiePlayer) for more information.

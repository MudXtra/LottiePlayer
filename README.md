[![Sponsor](https://img.shields.io/badge/Sponsor-❤️_GitHub_Sponsors-orange?logo=github&style=for-the-badge)](https://github.com/sponsors/versile2)

# Blazor Lottie Player

Blazor Lottie Player makes it easy to integrate high-quality Lottie animations into your Blazor applications.

## 🚀 Getting Started

### 1. Install the NuGet Package

Add [Blazor.Lottie.Player](https://mudx.org/docs/lottie) to your project via NuGet.

### 2. Use an Interactive Render Mode

`LottiePlayer` loads and controls the animation through JavaScript interop, so it only works on an **interactive** page. In a Blazor Web App (including the MudBlazor and Blazor templates, which render pages with static server rendering by default), a page that has no render mode shows an empty `<div>` and logs no error. Mark the page (or the whole app) interactive:

```razor
@page "/"
@rendermode InteractiveServer

<LottiePlayer Style="height: 200px;" Src="https://lottie.host/382bbcd7-e7b1-4a30-90cd-884a9c7c3bb1/F7I9nPKEr1.json" />
```

Use `InteractiveWebAssembly` or `InteractiveAuto` the same way. Interactive Server and Blazor WebAssembly apps that already run every page interactively need no change.

### 3. Choose How to Use the Component

There are two ways to use Blazor Lottie Player:

#### a. Using the Built-in Component

- Add `@using Blazor.Lottie.Player` to your `_Imports.razor` file.  
- Use the `<LottiePlayer>` component in your Razor files, setting the `Src` parameter to your Lottie animation JSON file.  
- The player will size itself to fill its parent container. Use CSS to control its width and height as needed.

#### b. Using the Module Programmatically (Advanced)

- Add `@using Blazor.Lottie.Player` to your `_Imports.razor` file.  
- Include the Lottie Web library (The LottiePlayer component loads it from the below CDN):

  ```html
  <script src="https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.13.0/lottie.min.js"
          integrity="sha512-uOtp2vx2X/5+tLBEf5UoQyqwAkFZJBM5XwGa7BfXDnWR+wdpRvlSVzaIVcRe3tGNsStu6UMDCeXKEnr4IBT8gA=="
          crossorigin="anonymous"></script>
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

### 4. Get Animations

Blazor Lottie Player plays animations; it doesn't ship any. Point `Src` at a Lottie JSON file you host or link to. Some places to find or make one:

- [LottieFiles](https://lottiefiles.com/free-animations): the largest library, with many free animations.
- [Lordicon](https://lordicon.com/): animated icons.
- [useAnimations](https://useanimations.com/): small UI micro-animations.
- Make your own in After Effects and export with [Bodymovin](https://github.com/airbnb/lottie-web).

Each animation comes with its own license from its creator, so check it before you ship. Blazor Lottie Player is not affiliated with Airbnb or LottieFiles.

For a working example, see the [Blazor Lottie Player documentation](https://mudx.org/docs/lottie).

### 5. Read the Docs

Visit the [Blazor Lottie Player documentation](https://mudx.org/docs/lottie) for detailed usage instructions, examples, and API reference.

### 6. Get Help, Support, or Contribute

If you need help, want to report an issue, or would like to support or contribute to the project, please visit the [GitHub repository](https://github.com/MudXtra/LottiePlayer) for more information.

using System.ComponentModel;

namespace Blazor.Lottie.Player
{
    /// <summary>
    /// Specifies the type of Lottie animation to render.
    /// </summary>
    public enum LottieAnimationType
    {
        /// <summary>
        /// Creates clean SVG elements with proper viewBox handling.
        /// </summary>
        /// <remarks>Best feature support and general purpose.</remarks>
        [Description("svg")]
        Svg,

        /// <summary>
        /// Uses 2D context, clears canvas each frame, handles sizing automatically.
        /// </summary>
        /// <remarks>Performance option for complex animations, typically better for full screen or
        /// large animations.</remarks>
        [Description("canvas")]
        Canvas,

        /// <summary>
        /// Creates HTML elements with CSS transforms.
        /// </summary>
        /// <remarks>Optional, generally slower with limited support for complex animations.</remarks>
        [Description("html")]
        Html
    }
}

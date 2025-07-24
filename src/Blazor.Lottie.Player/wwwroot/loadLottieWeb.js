export async function initialize(source) {
    try {
        await injectJsLibrary(source);
        return true;
    }
    catch (error) {
        console.error("Error initializing Lottie Web:", error);
        return false;
    }
}

export async function injectJsLibrary(source) {
    return new Promise((resolve, reject) => {
        if (document.querySelector("script[data-bodymovinjs]")) {
            // Already loaded
            return resolve();
        }
        // Check if lottie is already available globally
        if (window.lottie) {
            return resolve();
        }
        const script = document.createElement("script");
        script.setAttribute("data-bodymovinjs", "true");
        script.type = "text/javascript";
        script.src = source;

        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load ${source}`));

        document.head.appendChild(script);
    });
}

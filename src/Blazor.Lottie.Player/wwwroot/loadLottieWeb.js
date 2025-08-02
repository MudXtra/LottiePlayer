export async function initialize(source) {
    try {
        if (window.lottie && typeof window.lottie === "object") {
            // Lottie is already loaded
            return true;
        }
        await injectJsLibrary(source);
        if (window.lottie && typeof window.lottie === "object") {
            return true;
        }
        return false;
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
            if (window.lottie) {
                return resolve();
            }
            return waitForLottie(resolve, reject);
        }
        // Check if lottie is already available globally
        if (window.lottie) {
            return resolve();
        }
        const script = document.createElement("script");
        script.setAttribute("data-bodymovinjs", "true");
        script.type = "text/javascript";
        script.src = source;

        script.onload = () => waitForLottie(resolve, reject);
        script.onerror = () => reject(new Error(`Failed to load ${source}`));

        document.head.appendChild(script);
    });
}

function waitForLottie(resolve, reject) {
    const start = Date.now();
    const timeout = 1000;
    const interval = 25;

    function check() {
        if (window.lottie && typeof window.lottie === "object") {
            resolve();
        } else if (Date.now() - start >= timeout) {
            console.error(`Lottie did not become available within ${timeout}ms, continuing load.`);
            resolve();
        } else {
            setTimeout(check, interval);
        }
    }
    check();
}

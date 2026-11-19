// One in-flight load per source, shared by every player on the page. The ES module is a
// singleton, so a second player that arrives while a script tag is still pending awaits
// that same load instead of polling or injecting a duplicate tag.
const pendingLoads = new Map();

function hasLottie() {
    return !!window.lottie && typeof window.lottie === "object";
}

export async function initialize(source) {
    try {
        if (hasLottie()) {
            // Lottie is already loaded
            return true;
        }
        await injectJsLibrary(source);
        return hasLottie();
    }
    catch (error) {
        console.error("Error initializing Lottie Web:", error);
        return false;
    }
}

export function injectJsLibrary(source) {
    if (hasLottie()) {
        return Promise.resolve();
    }
    const pending = pendingLoads.get(source);
    if (pending) {
        return pending;
    }
    const load = new Promise((resolve, reject) => {
        const existing = document.querySelector("script[data-bodymovinjs]");
        if (existing) {
            // Another source is mid-load (or the tag came from elsewhere): follow that load, not a poll.
            const other = pendingLoads.values().next().value;
            return other ? other.then(resolve, reject) : waitForLottie(resolve, reject);
        }
        const script = document.createElement("script");
        script.setAttribute("data-bodymovinjs", "true");
        script.type = "text/javascript";
        script.src = source;

        script.onload = () => waitForLottie(resolve, (error) => {
            // loaded but never defined window.lottie: remove the tag so a fallback source can be injected
            script.remove();
            reject(error);
        });
        script.onerror = () => {
            // remove the failed tag so a fallback source can be injected
            script.remove();
            reject(new Error(`Failed to load ${source}`));
        };

        document.head.appendChild(script);
    });
    pendingLoads.set(source, load);
    const clear = () => pendingLoads.delete(source);
    load.then(clear, clear);
    return load;
}

function waitForLottie(resolve, reject) {
    const start = Date.now();
    const timeout = 1000;
    const interval = 25;

    function check() {
        if (hasLottie()) {
            resolve();
        } else if (Date.now() - start >= timeout) {
            reject(new Error(`Lottie did not become available within ${timeout}ms.`));
        } else {
            setTimeout(check, interval);
        }
    }
    check();
}

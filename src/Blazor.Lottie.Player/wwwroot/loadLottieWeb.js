// One in-flight load per source, shared by every player on the page. The ES module is a
// singleton, so a second player that arrives while a script tag is still pending awaits
// that same load instead of polling or injecting a duplicate tag. Only the player that
// injects a tag registers a load; followers never store theirs, so a failed tag cannot
// leave a stale entry that a fallback to another source would pick up.
const pendingLoads = new Map();
let currentTagLoad = null; // the single in-flight tag load (only one tag exists at a time)

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
    const existing = document.querySelector("script[data-bodymovinjs]");
    if (existing) {
        // Another source is mid-load (or the tag came from elsewhere): follow that load, not a poll.
        // If it fails, this source has not been tried yet, so try it once on its own.
        // A tag we did not inject (no tracked load) is only polled once: retrying here would loop forever.
        if (!currentTagLoad) return new Promise(waitForLottie);
        return currentTagLoad.catch(() => (hasLottie() ? undefined : injectJsLibrary(source)));
    }
    const load = new Promise((resolve, reject) => {
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
    currentTagLoad = load;
    const clear = () => {
        pendingLoads.delete(source);
        if (currentTagLoad === load) currentTagLoad = null;
    };
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

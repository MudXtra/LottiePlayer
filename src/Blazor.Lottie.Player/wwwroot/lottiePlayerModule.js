// Use a WeakMap for automatic cleanup when elements are garbage collected
const blazorLottiePlayerStore = new WeakMap();

export function initialize(dotNetRef, elementRef, options) {
    console.log(dotNetRef);
    console.log(elementRef);
    console.log(options);
    const animation = lottie.loadAnimation({
        container: elementRef,
        renderer: options.renderer,
        loop: options.loop,
        autoplay: false,
        path: options.path
    });
    console.log(animation);
    let success = animation !== null && animation !== undefined;
    console.log(success);
    if (!success) {
        console.error("Failed to load Lottie animation. Check the path and options.");
        return false;
    }

    // Store using elementRef as key - automatic cleanup when element is removed
    blazorLottiePlayerStore.set(elementRef, {
        animation: animation,
        dotNetRef: dotNetRef,
        options: options,
    });

    console.log(blazorLottiePlayerStore);

    // Set up options
    success = setOptions(elementRef);
    console.log(`setOptions: ${success}`)
    if (!success) {
        console.error("Failed to set options for Lottie animation.");
        return false;
    }
    // Set up event listeners
    success = addEvents(elementRef);
    console.log(`addEvents: ${success}`);
    if (!success) {
        console.error("Failed to add event listeners for Lottie animation.");
        return false;
    }
    // start playing the animation if autoplay is true
    if (options.autoplay) {
        // If autoplay is true, play the animation immediately
        animation.play();
        console.log("play");
    }
    return true; // Return success status
}

function setOptions(elementRef) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation && stored?.options) {
        // Apply various options
        const options = stored.options;
        if (options.speed !== undefined && options.speed !== 1.0) stored.animation.setSpeed(options.speed);
        if (options.direction !== undefined && options.direction !== 1) stored.animation.setDirection(options.direction);
        if (options.setSubFrame !== true) stored.animation.setSubframe(false);
        if (options.quality !== undefined && options.quality !== 'high') lottie.setQuality(options.quality);
        return true;
    }
    return false;
}

function addEvents(elementRef) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation && stored?.dotNetRef && stored?.options) {
        const animation = stored.animation;
        const dotNetAdapter = stored.dotNetRef;
        const options = stored.options;

        // Add event listeners
        animation.addEventListener('dataready', () => {
            const eventArgs = {
                elementId: elementRef.id,
                currentFrame: animation.currentFrame,
                totalFrames: animation.totalFrames
            };
            invokeDotNetMethodAsync(dotNetAdapter, "AnimationReadyEventAsync", eventArgs);
        });
        animation.addEventListener('DOMLoaded', () => {
            const eventArgs = {
                elementId: elementRef.id,
                currentFrame: animation.currentFrame,
                totalFrames: animation.totalFrames
            };
            invokeDotNetMethodAsync(dotNetAdapter, "DOMLoadedEventAsync", eventArgs);
        });
        animation.addEventListener('complete', () => {
            invokeDotNetMethodAsync(dotNetAdapter, 'CompleteEventAsync');
        });
        animation.addEventListener('loopComplete', () => {
            invokeDotNetMethodAsync(dotNetAdapter, 'LoopCompleteEventAsync');
        });
        if (options.enterFrameEvent) {
            animation.addEventListener('enterFrame', (e) => {
                invokeDotNetMethodAsync(dotNetAdapter, 'EnterFrameEventAsync', e);
            });
        }
        return true;
    }
    return false;
}

function invokeDotNetMethodAsync(dotNetAdapter, methodName, ...args) {
    return dotNetAdapter.invokeMethodAsync(methodName, ...args)
        .catch((reason) => {
            console.error(reason);
        });
}

export function play(elementRef) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation) {
        stored.animation.play();
        return true;
    }
    return false;
}

export function pause(elementRef) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation) {
        stored.animation.pause();
        return true;
    }
    return false;
}

export function stop(elementRef) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation) {
        stored.animation.stop();
        return true;
    }
    return false;
}

export function setSpeed(elementRef, speed) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation && speed) {
        stored.animation.setSpeed(speed);
        return true;
    }
    return false;
}

export function setDirection(elementRef, direction) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation && direction) {
        stored.animation.setDirection(direction);
        return true;
    }
    return false;
}

export function destroy(elementRef) {
    const stored = blazorLottiePlayerStore.get(elementRef);
    if (stored?.animation) {
        stored.animation.destroy();
        blazorLottiePlayerStore.delete(elementRef);
        return true;
    }
    return false;
}

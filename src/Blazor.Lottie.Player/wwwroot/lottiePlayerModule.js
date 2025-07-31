
export function initialize(dotNetRef, elementRef, options) {
    const animation = lottie.loadAnimation({
        container: elementRef,
        renderer: options.renderer,
        loop: options.loop,
        autoplay: false,
        path: options.path
    });
    let success = animation !== null && animation !== undefined;
    if (!success) {
        console.error("Failed to load Lottie animation. Check the path and options.");
        return null;
    }

    // Set up options
    success = setOptions(animation, options);
    if (!success) {
        console.error("Failed to set options for Lottie animation.");
        return null;
    }
    // Set up event listeners
    success = addEvents(animation, dotNetRef, options, elementRef);
    if (!success) {
        console.error("Failed to add event listeners for Lottie animation.");
        return null;
    }
    // start playing the animation if autoplay is true
    if (options.autoPlay) {
        // If autoplay is true, play the animation immediately
        animation.play();
    }
    return animation; // Return animation if successful
}

function setOptions(animation, options) {
    if (animation && options) {
        // Apply various options
        if (options.speed !== undefined && options.speed !== 1.0) animation.setSpeed(options.speed);
        if (options.direction !== undefined && options.direction !== 1) animation.setDirection(options.direction);
        if (options.setSubFrame !== true) animation.setSubframe(false);
        if (options.quality !== undefined && options.quality !== 'high') lottie.setQuality(options.quality);
        return true;
    }
    return false;
}

function addEvents(animation, dotNetAdapter, options, elementRef) {
    if (animation && dotNetAdapter && options && elementRef) {
        animation.addEventListener('data_ready', () => {
            // Add event listeners
            const eventArgs = {
                elementId: elementRef.id,
                currentFrame: animation.currentFrame,
                totalFrames: animation.totalFrames
            };
            invokeDotNetMethodAsync(dotNetAdapter, "AnimationReadyEvent", eventArgs);
        });
        animation.addEventListener('DOMLoaded', () => {
            // Add event listeners
            const eventArgs = {
                elementId: elementRef.id,
                currentFrame: animation.currentFrame,
                totalFrames: animation.totalFrames
            };
            invokeDotNetMethodAsync(dotNetAdapter, "DOMLoadedEvent", eventArgs);
        });
        animation.addEventListener('complete', () => {
            invokeDotNetMethodAsync(dotNetAdapter, 'CompleteEvent');
        });
        animation.addEventListener('loopComplete', () => {
            invokeDotNetMethodAsync(dotNetAdapter, 'LoopCompleteEvent');
        });
        if (options.enterFrameEvent) {
            animation.addEventListener('enterFrame', (e) => {
                invokeDotNetMethodAsync(dotNetAdapter, 'EnterFrameEvent', e);
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

export function play(animation) {
    if (animation) {
        animation.play();
    } else {
        console.error("Animation instance is null or undefined.");
    }
}

export function stop(animation) {
    if (animation) {
        animation.stop();
    } else {
        console.error("Animation instance is null or undefined.");
    }
}

export function pause(animation) {
    if (animation) {
        animation.pause();
    } else {
        console.error("Animation instance is null or undefined.");
    }
}

export function goToAndStop(animation, frame, isFrame) {
    if (animation) {
        if (isFrame) {
            animation.goToAndStop(frame, true);
        } else {
            animation.goToAndStop(animation.totalFrames * frame, true);
        }
    } else {
        console.error("Animation instance is null or undefined.");
    }
}

export function goToAndPlay(animation, frame, isFrame) {
    if (animation) {
        if (isFrame) {
            animation.goToAndPlay(frame, true);
        } else {
            animation.goToAndPlay(animation.totalFrames * frame, true);
        }
    } else {
        console.error("Animation instance is null or undefined.");
    }
}

export function setDirection(animation, direction) {
    if (animation && (direction === 1 || direction === -1)) {
        animation.setDirection(direction);
    } else {
        console.error("Invalid animation instance or direction value. Direction must be 1 or -1.");
    }
}

export function setSpeed(animation, speed) {
    if (animation && speed > 0) {
        animation.setSpeed(speed);
    } else {
        console.error("Invalid animation instance or speed value. Speed must be greater than 0.");
    }
}

export function destroy(animation) {
    if (animation) {
        animation.destroy();
    } else {
        console.error("Animation instance is null or undefined.");
    }
}

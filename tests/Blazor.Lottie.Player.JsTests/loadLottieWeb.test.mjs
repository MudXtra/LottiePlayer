// Run: node --test tests/Blazor.Lottie.Player.JsTests
// Models script-tag loading with a stub DOM; the loader is the real wwwroot/loadLottieWeb.js.
import { test } from "node:test";
import assert from "node:assert/strict";
import { pathToFileURL } from "node:url";
import path from "node:path";

const loaderUrl = pathToFileURL(path.resolve(import.meta.dirname, "../../src/Blazor.Lottie.Player/wwwroot/loadLottieWeb.js")).href;
let n = 0;

// behaviours: map of source prefix -> { delay, outcome: "load" | "error" | "load-nolottie" }
async function setup(behaviours, preexisting = []) {
    const scripts = [...preexisting];
    globalThis.window = globalThis;
    delete globalThis.lottie;
    globalThis.document = {
        querySelector: () => scripts.find((s) => s.attached) ?? null,
        createElement: () => ({ setAttribute() {}, remove() { this.attached = false; } }),
        head: {
            appendChild(s) {
                s.attached = true;
                scripts.push(s);
                const b = behaviours.find((x) => s.src.startsWith(x.prefix));
                setTimeout(() => {
                    if (b.outcome === "error") s.onerror();
                    else {
                        if (b.outcome === "load") globalThis.lottie = {};
                        s.onload();
                    }
                }, b.delay);
            },
        },
    };
    console.error = () => {};
    const mod = await import(`${loaderUrl}?t=${n++}`); // fresh module state per test
    return { mod, scripts };
}

const CDN = "https://cdn/lottie.min.js";
const LOCAL = "./_content/lottie.min.js";

// mirrors LottiePlayer.OnAfterRenderAsync: CDN once, then local once
async function player(mod, delay) {
    await new Promise((r) => setTimeout(r, delay));
    let ok = await mod.initialize(CDN);
    if (!ok) ok = await mod.initialize(LOCAL);
    return ok;
}

test("slow CDN success: every player loads and one tag is injected", async () => {
    const { mod, scripts } = await setup([{ prefix: "https://", delay: 2500, outcome: "load" }]);
    const results = await Promise.all([player(mod, 0), player(mod, 10), player(mod, 20)]);
    assert.deepEqual(results, [true, true, true]);
    assert.equal(scripts.length, 1);
});

test("slow CDN failure with local fallback: every player loads", async () => {
    const { mod, scripts } = await setup([
        { prefix: "https://", delay: 2500, outcome: "error" },
        { prefix: "./", delay: 100, outcome: "load" },
    ]);
    const results = await Promise.all([player(mod, 0), player(mod, 10), player(mod, 20)]);
    assert.deepEqual(results, [true, true, true]);
    assert.equal(scripts.length, 2); // one CDN tag, one local tag
});

test("both sources fail: all players fail and the load is bounded", async () => {
    const { mod } = await setup([
        { prefix: "https://", delay: 300, outcome: "error" },
        { prefix: "./", delay: 300, outcome: "error" },
    ]);
    const t0 = Date.now();
    const results = await Promise.all([player(mod, 0), player(mod, 10)]);
    assert.deepEqual(results, [false, false]);
    assert.ok(Date.now() - t0 < 3000);
});

test("script loads but window.lottie never appears: tag removed so local fallback can inject", async () => {
    const { mod, scripts } = await setup([
        { prefix: "https://", delay: 10, outcome: "load-nolottie" },
        { prefix: "./", delay: 10, outcome: "load" },
    ]);
    assert.equal(await player(mod, 0), true);
    assert.equal(scripts.length, 2);
    assert.equal(scripts[0].attached, false);
});

test("N1: CDN-primary player falls back inline while a local-primary player follows the CDN tag: both load", async () => {
    const { mod, scripts } = await setup([
        { prefix: "https://", delay: 200, outcome: "error" },
        { prefix: "./", delay: 50, outcome: "load" },
    ]);
    const a = (async () => (await mod.initialize(CDN)) || (await mod.initialize(LOCAL)))();
    await new Promise((r) => setTimeout(r, 10));
    const b = mod.initialize(LOCAL); // follows A's in-flight CDN tag
    assert.deepEqual(await Promise.all([a, b]), [true, true]);
    assert.equal(scripts.length, 2);
});

test("P4: a foreign data-bodymovinjs tag that never yields lottie: initialize terminates (no unbounded retry)", async () => {
    const foreign = { src: "https://consumer/own.js", attached: true }; // not ours, never removed
    const { mod, scripts } = await setup([], [foreign]);
    const settled = await Promise.race([
        mod.initialize(CDN).then((v) => ({ v })),
        new Promise((r) => setTimeout(() => r(null), 4500)),
    ]);
    assert.ok(settled, "initialize still pending after 4.5s");
    assert.equal(settled.v, false);
    assert.equal(scripts.length, 1, "no tag injected next to the foreign one");
});

const HASH = "sha512-uOtp2vx2X/5+tLBEf5UoQyqwAkFZJBM5XwGa7BfXDnWR+wdpRvlSVzaIVcRe3tGNsStu6UMDCeXKEnr4IBT8gA==";

test("SRI: a source with an integrity value gets integrity and crossorigin=anonymous", async () => {
    const { mod, scripts } = await setup([{ prefix: "https://", delay: 10, outcome: "load" }]);
    assert.equal(await mod.initialize(CDN, HASH), true);
    assert.equal(scripts.length, 1);
    assert.equal(scripts[0].integrity, HASH);
    assert.equal(scripts[0].crossOrigin, "anonymous");
});

test("SRI: a custom source without an integrity value gets neither attribute", async () => {
    const { mod, scripts } = await setup([{ prefix: "https://", delay: 10, outcome: "load" }]);
    assert.equal(await mod.initialize("https://other.example/lottie.js", undefined), true);
    assert.equal(scripts.length, 1);
    assert.equal(scripts[0].integrity, undefined);
    assert.equal(scripts[0].crossOrigin, undefined);
});

test("SRI: an empty integrity value is treated as none", async () => {
    const { mod, scripts } = await setup([{ prefix: "https://", delay: 10, outcome: "load" }]);
    assert.equal(await mod.initialize(CDN, ""), true);
    assert.equal(scripts[0].integrity, undefined);
    assert.equal(scripts[0].crossOrigin, undefined);
});

test("SRI: an integrity failure (script error) falls back to the local copy, which carries no integrity", async () => {
    const { mod, scripts } = await setup([
        { prefix: "https://", delay: 10, outcome: "error" },
        { prefix: "./", delay: 10, outcome: "load" },
    ]);
    // mirrors LottiePlayer.OnAfterRenderAsync: configured source with its integrity, then the local copy once
    let ok = await mod.initialize(CDN, HASH);
    assert.equal(ok, false);
    ok = await mod.initialize(LOCAL);
    assert.equal(ok, true);
    assert.equal(scripts.length, 2);
    assert.equal(scripts[0].integrity, HASH);
    assert.equal(scripts[1].src, LOCAL);
    assert.equal(scripts[1].integrity, undefined);
    assert.equal(scripts[1].crossOrigin, undefined);
});

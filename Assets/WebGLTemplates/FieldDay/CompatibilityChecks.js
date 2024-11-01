function DetectWebGLSupport() {

    if (!WebGLRenderingContext) {
        return false;
    }

    var canvas = document.createElement('canvas');
    var webGLConfig = { powerPreference: 'high-performance' };
    var gl;
    try {
        gl = canvas.getContext('webgl', webGLConfig) || canvas.getContext('experimental-webgl', webGLConfig);
    } catch(e) {
    }

    canvas.remove();
    return gl instanceof WebGLRenderingContext;
}

function DetectBrowserSupport() {
    var userAgent = navigator.userAgent;
    if (/iPhone|iPad|iPod|Android/i.test(userAgent)) {
        return "mobile";
    } else if (/Mac/i.test(userAgent) && navigator.maxTouchPoints && navigator.maxTouchPoints > 1) {
        return "mobile";
    } else if (/MSIE|Trident\//i.test(userAgent)) {
        return "ie";
    }else if (!DetectWebGLSupport()) {
        return "no-webgl";
    } else {
        return "supported";
    }
}
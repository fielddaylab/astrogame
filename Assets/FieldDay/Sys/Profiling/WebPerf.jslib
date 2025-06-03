var WebPerf = {
    WebPerf_RetrieveGraphicsDeviceID__sig: 'i',
    WebPerf_RetrieveGraphicsDeviceID: function () {
        var gpuInfo = Module.SystemInfo.gpu;
        var angleTest = /ANGLE \(.*?\((0x[\dabcdef]+)\)/i.exec(gpuInfo);
        if (angleTest.length >= 2) {
            return parseInt(angleTest[1]);
        }
        return 0;
    },

    WebPerf_IsCrossOriginIsolated__sig: 'i',
    WebPerf_IsCrossOriginIsolated: function() {
        return window.crossOriginIsolated;
    }
};

mergeInto(LibraryManager.library, WebPerf);
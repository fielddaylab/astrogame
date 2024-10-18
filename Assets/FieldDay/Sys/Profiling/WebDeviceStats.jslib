var WebDeviceStatsLib = {
    WebDeviceStats_RetrieveGraphicsDeviceID: function () {
        var gpuInfo = Module.SystemInfo.gpu;
        var angleTest = /ANGLE \(.*?\((0x[\dabcdef]+)\)/i.exec(gpuInfo);
        if (angleTest.length >= 2) {
            return parseInt(angleTest[1]);
        }
        return 0;
    }
};

mergeInto(LibraryManager.library, WebDeviceStatsLib);
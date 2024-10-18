var NativeWebAudio = {
    NativeWebAudio_WakeUp: function() {
        if (WEBAudio.audioContext.state === "suspended") {
            WEBAudio.audioContext.resume();
            return true;
        } else {
            return false;
        }
    },

    NativeWebAudio_IsActive: function () {
        return WEBAudio.audioContext.state !== "suspended";
    }
};

mergeInto(LibraryManager.library, NativeWebAudio);
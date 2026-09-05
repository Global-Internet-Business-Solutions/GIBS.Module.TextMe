/* Module Script */
var GIBS = GIBS || {};

GIBS.TextMe = {
    playTone: function () {
        try {
            var audioContext = new (window.AudioContext || window.webkitAudioContext)();
            var oscillator = audioContext.createOscillator();
            var gain = audioContext.createGain();
            oscillator.type = "sine";
            oscillator.frequency.setValueAtTime(880, audioContext.currentTime);
            gain.gain.setValueAtTime(0.08, audioContext.currentTime);
            oscillator.connect(gain);
            gain.connect(audioContext.destination);
            oscillator.start();
            oscillator.stop(audioContext.currentTime + 0.12);
        }
        catch (e) {
        }
    }
};

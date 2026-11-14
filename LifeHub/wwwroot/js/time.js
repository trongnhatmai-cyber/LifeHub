/* =====================================================================
   LIFEHUB - TIME & POMODORO JS
   Đếm ngược thời gian tập trung 25 phút, tạm dừng và đặt lại
   ===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    let timer = null;
    let totalSeconds = 25 * 60; // 25 phút
    let isRunning = false;

    const display = document.getElementById("pomodoroDisplay");
    const startBtn = document.getElementById("pomoStartBtn");
    const resetBtn = document.getElementById("pomoResetBtn");
    const modePill = document.getElementById("pomoModePill");

    function renderTime() {
        if (!display) return;
        const mins = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
        const secs = String(totalSeconds % 60).padStart(2, "0");
        display.textContent = `${mins}:${secs}`;
    }

    if (startBtn) {
        startBtn.addEventListener("click", () => {
            if (isRunning) {
                // Pause
                clearInterval(timer);
                isRunning = false;
                startBtn.textContent = "▶ Tiếp tục";
            } else {
                // Start
                isRunning = true;
                startBtn.textContent = "⏸ Tạm dừng";
                timer = setInterval(() => {
                    if (totalSeconds > 0) {
                        totalSeconds--;
                        renderTime();
                    } else {
                        clearInterval(timer);
                        isRunning = false;
                        alert("🎉 Chúc mừng bạn đã hoàn thành 25 phút tập trung cao độ!");
                        totalSeconds = 5 * 60; // Nghỉ 5 phút
                        if (modePill) modePill.textContent = "NGHỈ GIẢI LAO 5P";
                        startBtn.textContent = "▶ Bắt đầu nghỉ";
                        renderTime();
                    }
                }, 1000);
            }
        });
    }

    if (resetBtn) {
        resetBtn.addEventListener("click", () => {
            clearInterval(timer);
            isRunning = false;
            totalSeconds = 25 * 60;
            if (modePill) modePill.textContent = "TẬP TRUNG 25 PHÚT";
            if (startBtn) startBtn.textContent = "▶ Bắt đầu";
            renderTime();
        });
    }

    renderTime();
});

/* =====================================================================
   LIFEHUB - TRỢ LÝ AI (AI ASSISTANT)               wwwroot/js/ai.js
   Xử lý gửi câu hỏi tới AIController/Ask, hiển thị hội thoại và chip gợi ý nhanh.
   ===================================================================== */
(function () {
    "use strict";

    var page = document.querySelector(".ai-page");
    if (!page) return;

    var askUrl = page.getAttribute("data-ask-url");
    var form = document.getElementById("aiChatForm");
    var input = document.getElementById("aiPromptInput");
    var log = document.getElementById("aiChatLog");
    var tokenInput = document.querySelector("#aiTokenForm input[name='__RequestVerificationToken']");

    function appendMsg(role, text, extraClass) {
        if (!log) return null;
        var div = document.createElement("div");
        div.className = "ai-msg " + role + (extraClass ? " " + extraClass : "");
        div.textContent = text;
        log.appendChild(div);
        log.scrollTop = log.scrollHeight;
        return div;
    }

    function sendPrompt(promptText) {
        var text = (promptText || "").trim();
        if (!text || !askUrl || !tokenInput) return;

        appendMsg("user", text);
        if (input) input.value = "";

        var typingEl = appendMsg("bot", "Đang tổng hợp dữ liệu của bạn...", "typing");

        fetch(askUrl, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": tokenInput.value
            },
            body: JSON.stringify({ prompt: text }),
            credentials: "same-origin"
        })
        .then(function (r) { return r.ok ? r.json() : Promise.reject(); })
        .then(function (data) {
            if (typingEl) typingEl.remove();
            appendMsg("bot", (data && data.reply) ? data.reply : "Tôi đã ghi nhận yêu cầu của bạn.");
        })
        .catch(function () {
            if (typingEl) typingEl.remove();
            appendMsg("bot", "Không thể xử lý yêu cầu lúc này. Vui lòng thử lại.");
        });
    }

    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            sendPrompt(input ? input.value : "");
        });
    }

    page.querySelectorAll(".ai-chip[data-prompt]").forEach(function (chip) {
        chip.addEventListener("click", function () {
            sendPrompt(chip.getAttribute("data-prompt"));
        });
    });
})();

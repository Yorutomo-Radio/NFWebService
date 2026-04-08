const dialog = document.getElementById("check-send-letter");
const submit_btn = document.getElementById("real-submit");
const limiter = true; // 送信制限を有効にするかどうか (1時間に1回の制限)
const anonymous = ["夜の住人", "月明かりの誰か", "秘密のペンネーム", "UNKNOWN HOST", "1万光年先の誰か"];
const letterType = ["normal", "favorite", "request", "advice", "vrchat", "voice"];
const letterTypeExample = {
    "normal": "🌙 夜風便り - なんでもOK！近況・エピソード・一言など!",
    "favorite": "✨ すきって言わせて - ハマってるもの・推しを語りたい",
    "request": "📻 リクエスト - 聴きたい話題・やってほしいコーナーなど",
    "advice": "🌃 夜のお悩み放送局 - モヤモヤ・相談・茶化してほしいこと",
    "vrchat": "🥽 今夜のVRC通信 - VRChatの出来事・ワールド・フレンド話",
    "voice": "📢 ボイス - シチュエーションボイスを頼んでみましょう"
};
let selectedLetterType = null;
let isSendingLetter = false;
let nameInput = document.getElementById('name');
let messageInput = document.getElementById("message");


function selectLetterType(type) {
    letterType.forEach(element => {
        const button = document.getElementById(`type-${element}`);

        if (element === type) {
            console.log(`Selected letter type: ${type}`);
            selectedLetterType = type;
            button.classList.add("selected");
            button.style.background = "linear-gradient(45deg, #2e2e83, #38385F)"; // 選択されたボタンの背景色を変更
        } else {
            if (button.classList.contains("selected")) {
                button.classList.remove("selected");
            }
            button.style.background = "#1B1B36A1"; // 背景色をリセット
        }
    });

    // 例文を更新
    let exampleText = letterTypeExample[type];
    document.querySelector(".form-type-example").textContent = `"${exampleText}"`;
}


function checkSendLetter() {
    let checkNameElement = document.getElementById('dialog-name');
    let checkMessageType = document.getElementById("dialog-type");


    // 文字数超過チェック
    if (nameInput.value.length >= 128) {
        TextTooLong(`エラー!: ラジオネーム部分に入力された文字数が多すぎます! 目標: <128 / 現在: ${nameInput.value.length}`);
        return;
    } else if (messageInput.value.length >= 1024) {
        TextTooLong(`エラー!: お便り部分に入力された文字数が多すぎます! 目標: <1024 / 現在: ${messageInput.value.length}`);
        return;
    } else if (!selectedLetterType) {
        alert("お便りの種類を選択してください。");
        return;
    } else {
        submit_btn.hidden = false;
    }

    // 空白の場合送信不可に
    if (messageInput.value.trim() === "") {
        alert("お便り部分を空白で送ることはできません。")
        return;
    }

    switch (selectedLetterType) {
        case "normal":
            checkMessageType.textContent = "お便りの種類: 夜風便り";
            break;
        case "favorite":
            checkMessageType.textContent = "お便りの種類: すきって言わせて";
            break;
        case "request":
            checkMessageType.textContent = "お便りの種類: リクエスト";
            break;
        case "advice":
            checkMessageType.textContent = "お便りの種類: 夜のお悩み放送局";
            break;
        case "vrchat":
            checkMessageType.textContent = "お便りの種類: 今夜のVRC通信";
            break;
        case "voice":
            checkMessageType.textContent = "お便りの種類: ボイス";
            break;
        default:
            alert("不明なお便りの種類が選択されました。");
            return;
    }

    if (nameInput.value && nameInput.value.trim() !== "") {
        checkNameElement.textContent = `ラジオネーム: ${nameInput.value}`;
    } else {
        let randomName = anonymous[Math.floor(Math.random() * anonymous.length)];
        checkNameElement.textContent = `ラジオネーム: ${randomName}(匿名)`;
    }

    document.getElementById("dialog-message").textContent = `メッセージ: ${messageInput.value}`;

    dialog.showModal();
}

function TextTooLong(msg) {
    alert(msg);
    submit_btn.hidden = true;
}


function submitLetter() {
    console.log(isSendingLetter);
    if (isSendingLetter) return;

    // 送信中フラグを立てる
    isSendingLetter = true;
    submit_btn.disabled = true;

    const lastSent = localStorage.getItem('lastSentTime');
    const now = new Date().getTime();

    if (lastSent && now - lastSent < 3600000 && limiter) {
        alert("少し時間を置いてから送ってください。");
        isSendingLetter = false;
        submit_btn.disabled = false;
        return;
    }


    const nameInput = document.getElementById('name');
    const messageInput = document.getElementById('message');

    // ダイアログで決定した名前を取得
    const finalName = nameInput.value.trim() !== ""
        ? nameInput.value
        : document.getElementById('dialog-name').textContent.replace('ラジオネーム: ', '').replace('(匿名)', '');

    const payload = {
        name: finalName,
        type: selectedLetterType,
        message: messageInput.value
    };

    submit_btn.hidden = true;

    fetch("https://api.yorutomo-radio.com/letter", {
        method: "POST",
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
    })
        .then(response => {
            if (response.ok) {
                alert("お便りを封筒に入れました。届くまで少しお待ちくださいね。");
                localStorage.setItem('lastSentTime', now);
                dialog.close();
                document.getElementById('letter-form').reset();

            } else {
                isSendingLetter = false;
                submit_btn.disabled = false;
                alert("送信に失敗しました。");
            }
        })
        .catch(error => {
            alert("エラーが発生しました。:" + error);
            isSendingLetter = false;
            submit_btn.disabled = false;
        })
        .finally(() => {
            if (!dialog.open) {
                isSendingLetter = false;
                submit_btn.disabled = false;
                submit_btn.hidden = false;
                submit_btn.textContent = "送信する";
            }
        });
}
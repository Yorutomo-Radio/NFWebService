const parent = document.querySelectorAll('.title-icon');
const grid = document.getElementById('host-list');
const version = "beta-v1.00";
let data;

// 初期生成
generateSound();
nowVersion();
checkLiveStatus();
generateStars();


function generateSound() {
    // 音声波形部分
    for (let n = 0; n < parent.length; n++) {
        for (let i = 1; i <= 4; i++) {
            const icon = document.createElement('p');
            icon.className = 'icon-' + i;
            parent[n].prepend(icon);
        }
    }
}

async function nowVersion() {
    let web_version = document.getElementById("version");
    web_version.textContent = `© 2026 夜友Radio ${version}`;

    const latestVersion = await getVersion();

    if (version !== latestVersion) {
        alert(`新しいバージョン [新:${latestVersion} 現在: ${version}] がリリースされました！\nCTRL + F5 で最新の状態に更新してください。`);
    }
}

async function getVersion() {
    try {
        const response = await fetch('https://api.yorutomo-radio.com/version');
        const data = await response.json();
        console.log(`最新バージョン: ${data.message}`);
        return data.message;
    } catch (err) {
        console.error("バージョン取得失敗:", err);
        return version;
    }
}

// ライブ状態をチェックする関数を定義
async function checkLiveStatus() {

    try {
        const response = await fetch('https://api.yorutomo-radio.com/islive');
        data = await response.json();
        const liveIndicator = document.getElementById('information');

        console.log(`ライブ状態: ${data.title} - ${data.islive} - ${data.start}`);

        if (data.islive === true) {
            liveIndicator.innerHTML = `
                <a href="${data.url}" target="_blank" rel="noopener noreferrer">
                    <span class="on-air">● ON AIR</span>
                    <span id="live-info">${data.title} - <span id="live-timer">00:00:00</span></span>
                </a>
            `;
            liveIndicator.classList.add('on-air');
        } else {
            liveIndicator.textContent = '';
            liveIndicator.classList.remove('on-air');
        }
    } catch (err) {
        console.error("ライブ状態取得失敗:", err);
    }
}

function generateStars() {
    const canvas = document.createElement('canvas');
    canvas.style.cssText = `
        position: fixed;
        inset: 0;
        width: 100%;
        height: 100%;
        z-index: -1;
        pointer-events: none;
    `;
    document.body.prepend(canvas);

    const ctx = canvas.getContext('2d');
    let stars = [];
    let shootingStars = [];
    let t = 0;

    function resize() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        createStars();
    }

    function createStars() {
        stars = [];
        const count = Math.floor((canvas.width * canvas.height) / 1000); // 密度調整
        for (let i = 0; i < count; i++) {
            stars.push({
                x: Math.random() * canvas.width,
                y: Math.random() * canvas.height,
                r: Math.random() * 1.2 + 0.3,  // 半径
                base: Math.random(), // またたきのベース
                speed: Math.random() * 0.008 + 0.002, // またたき速度
                phase: Math.random() * Math.PI * 2 // またたきの位相
            });
        }
    }

    function createShootingStar() {
        if (Math.random() > 0.02) return;

        shootingStars.push({
            x: Math.random() * canvas.width,
            y: Math.random() * canvas.height * 0.75,
            len: Math.random() * 80 + 40, // 尻尾の長さ 40〜120px
            speed: Math.random() * 6 + 4, // 速さ 4〜10px/フレーム
            opacity: 1, // 最初は不透明
            angle: Math.random() * Math.PI / 4 + Math.PI / 4
        });
    }

    function draw() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);  // 背景をクリア
        t += 1;

        stars.forEach(s => {
            const opacity = 0.3 + 0.7 * ((Math.sin(t * s.speed + s.phase) + 1) / 2); // sin波で透明度を変化
            ctx.beginPath();  // パス作成
            ctx.arc(s.x, s.y, s.r, 0, Math.PI * 2); // 円を定義 [x, y, 半径, 開始角, 終了角]
            ctx.fillStyle = `rgba(255, 255, 255, ${opacity})`; // 色をセット
            ctx.fill(); // 塗りつぶして描画
        });

        createShootingStar();
        shootingStars = shootingStars.filter(ss => ss.opacity > 0); // 消えた流れ星を掃除

            // 線を引く (頭から尻尾まで)
        shootingStars.forEach(ss => {
            ctx.beginPath();
            ctx.moveTo(ss.x, ss.y); // 頭 (明るい側)
            ctx.lineTo(
                ss.x - Math.cos(ss.angle) * ss.len, // 尻尾のX
                ss.y - Math.sin(ss.angle) * ss.len // 尻尾のY
            );

            // 頭>尻尾でグラデーション (白→透明)
            const grad = ctx.createLinearGradient(
                ss.x, ss.y,
                ss.x - Math.cos(ss.angle) * ss.len, // グラデ開始 (頭)
                ss.y - Math.sin(ss.angle) * ss.len // グラデ終了 (尻尾)
            );
            grad.addColorStop(0, `rgba(255,255,255,${ss.opacity})`);  // 頭: 白
            grad.addColorStop(1, 'transparent'); // 尻尾: 透明
            ctx.strokeStyle = grad;
            ctx.lineWidth = 1.5;
            ctx.stroke();

            ss.x += Math.cos(ss.angle) * ss.speed; // x座標を更新
            ss.y += Math.sin(ss.angle) * ss.speed; // y座標を更新
            ss.opacity -= 0.015; // 徐々に透明にしていく
        });

        requestAnimationFrame(draw); // 次フレームでdrawを呼び出す
    }

    window.addEventListener('resize', resize); // 画面サイズ変更時にリサイズ
    resize(); // 最初に1回呼んでcanvasサイズと星を初期化
    draw();   // アニメーション開始
}



// 常時実行
setInterval(checkLiveStatus, 10000);

setInterval(function updateLiveInfo() {
    const timeElement = document.getElementById('live-timer');

    if (!data || !timeElement || !data.start) return;

    const startTime = new Date(`${data.start}+09:00`);
    const nowTime = new Date();

    let elapsedTime = Math.floor((nowTime.getTime() - startTime.getTime()) / 1000);
    elapsedTime = Math.max(0, elapsedTime);

    let hour = Math.floor(elapsedTime / 3600);
    let min = Math.floor((elapsedTime % 3600) / 60);
    let sec = elapsedTime % 60;

    const format = (n) => String(n).padStart(2, '0');
    timeElement.textContent = `${format(hour)}:${format(min)}:${format(sec)}`;
}, 1000);
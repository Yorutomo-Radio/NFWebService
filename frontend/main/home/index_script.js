// アイコンSVG
const twitter_icon = `<svg class="twitter-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 271"><path d="m236 0h46l-101 115 118 156h-92.6l-72.5-94.8-83 94.8h-46l107-123-113-148h94.9l65.5 86.6zm-16.1 244h25.5l-165-218h-27.4z"/></svg>`
let html = "";

async function loadJson() {
    const response = await fetch('https://api.yorutomo-radio.com/hosts');
    const data = await response.json();

    let twitter_id = "";

    data.hosts.forEach(element => {
        if (element.twitter !== undefined) {
            let parts = element.twitter.split("https://x.com/");
            twitter_id = parts[1];
        } else {
            twitter_id = undefined;
        }

        let image = element.image + ".webp";
        let twitter_link;

        if (element.twitter === undefined) {
            html = `
                    <!--${element.name} PROFILE-->
                    <div class="host-profile">
                        <img src="${image}" alt="パーソナリティ画像">
                        <div>
                            <h3>${element.name}</h3>
                            <p>${element.description}</p>
                        </div>
                    </div>
            `;
        } else {
            twitter_link = element.twitter;
            html = `
                    <!--${element.name} PROFILE-->
                    <div class="host-profile">
                        <img src="${image}" alt="パーソナリティ画像">
                        <div>
                            <h3>${element.name}</h3>
                            <p>${element.description}</p>
                            <div class="sns">
                                <a href="${twitter_link}">
                                    <span>${twitter_icon}</span>
                                    <div></div>
                                    <p>@${twitter_id}</p>
                                </a>
                            </div>
                        </div>
                    </div>
            `;
        }

        grid.insertAdjacentHTML('beforeend', html);
    });
}

loadJson();
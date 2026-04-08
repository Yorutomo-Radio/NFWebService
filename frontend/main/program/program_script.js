const PROGRAM_ITEMS = document.getElementById("program-list");
const DATE = new Date();
const WEEKID = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
let html = "";

const loadProgramJson = async () => {
    const response = await fetch('https://api.yorutomo-radio.com/program');
    const data = await response.json();

    data.programs.forEach(element => {
    console.log(element);


        if (element === undefined) {
            html = `
            <h2>現在予定されている放送はありません。決まるまでしばらくお待ちください!</h2>
            `;

        } else {

            const week = new Date(`${DATE.getFullYear()} ${element.date}`).getDay();

            html = `
            <!--ID: ${element.id}-->
            <div class="program-item">
                <div class="program-date">
                    <h4 class="program-day">${element.date}<span class="${WEEKID[week]}">(${WEEKID[week]})</span></h4>
                    <h2 class="program-time">${element.time}</h2>
                </div>

                <img src="../assets/images/program/radio_1.png" alt="サムネイル" class="program-image" />

                <div class="program-text">
                    <h2 class="program-name">${element.title}</h2>
                    <p class="program-description">${element.description}</p>
                    <p class="program-host">メインパーソナリティー: ${element.host}</p>
                </div>
            </div>
            `;
        }

        PROGRAM_ITEMS.insertAdjacentHTML('beforeend', html);
    });
};

loadProgramJson();

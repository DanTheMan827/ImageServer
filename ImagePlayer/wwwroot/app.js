setInterval(() => {
    var ele = document.getElementById("components-reconnect-modal");
    if (`${ele?.innerText}`.indexOf("to restore functionality") != -1) {
        location.reload();
    } else if (`${ele?.innerText}`.indexOf("Reconnection failed.") != -1) {
        ele.getElementsByTagName("button")[0].click()
    }
}, 1000);
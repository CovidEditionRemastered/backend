const mqtt = require('mqtt')
const axios = require('axios');

const client = mqtt.connect('mqtt://node02.myqtthub.com', {
    clientId: 'Middleware_Server',
    username: 'middleware',
    password: 'password',
})

client.on('connect', function () {
    client.subscribe('esp32/update', function (err) {
        console.log("subscribed");
    })
})

client.on('message', function (topic, message) {
    // message is Buffer
    const m = message.toString().split(":");

    const uuid = m[0];
    const val = m[1].toLowerCase() === "true" ? 'true' : 'false';

    axios.post(`http://app/Hardware/${uuid}?state=${val}`).then(() => console.log("succeeded"));
})
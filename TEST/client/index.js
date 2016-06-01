const util = require('util');
const events = require('events');
const net = require('net');
const robot = require('robotjs');
const Composer = require('stream-pkg');

const listenPort = 6231;
var count = 0;
var screensize = robot.getScreenSize();
var composer = new Composer();

const client = net.connect({
    port: listenPort,
    host: '192.168.1.101'
}, function() {
    console.log("client connect.");
    client.setNoDelay();
    sendMousePosition();
});

function sendMousePosition() {
    count++;
    if (count == 50) {
        count = 0;
        client.write(composer.compose(JSON.stringify({
            type: 'click'
        })));
    } else {
        var mousePos = robot.getMousePos();
        var mousePosPercent = {
            x: ((mousePos.x / screensize.width) * 100).toFixed(2),
            y: ((mousePos.y / screensize.height) * 100).toFixed(2)
        }
        client.write(composer.compose(JSON.stringify({
            type: 'move',
            pos: mousePosPercent
        })));
    }

    setTimeout(function() {
        sendMousePosition();
    }, 100)
}

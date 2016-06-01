const util = require('util');
const events = require('events');
const net = require('net');
const robot = require('robotjs');

const listenPort = 6231;
var count = 0;
var screensize = robot.getScreenSize();

const client = net.connect({ port: listenPort }, function() {
    console.log("client connect.");
    sendMousePosition();
});

function sendMousePosition() {
    count++;
    if (count == 50) {
        count = 0;
        client.write(JSON.stringify({type: 'click'}));
    } else {
        var mousePos = robot.getMousePos();
        var mousePosPercent = {
        	x: ((mousePos.x /screensize.width)*100).toFixed(2),
        	y: ((mousePos.y /screensize.height)*100).toFixed(2)
        }
        client.write(JSON.stringify({type: 'move', pos: mousePosPercent}));
    }

    setTimeout(function() {
        sendMousePosition();
    }, 100)
}

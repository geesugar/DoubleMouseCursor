const util = require('util');
const events = require('events');
const net = require('net');
const robot = require('robotjs');
var Composer = require('stream-pkg');


const listenPort = 6231;
var mouseServer = null;
var mouseSocket = null;
var screensize = robot.getScreenSize();

function processMsg(pkg) {
	console.log("pkg "  + JSON.stringify(pkg));
    if (pkg.type === 'click') {
        robot.mouseClick();
    } else if (pkg.type === 'move') {
        var x = Math.round((screensize.width * pkg.pos.x) / 100);
        var y = Math.round((screensize.height * pkg.pos.y) / 100);
        robot.moveMouse(x, y);
    }
}

function processMsgs(pkgs) {
	for (var i = 0; i < pkgs.length; i++) {
		processMsg(pkgs[i]);
	}
}

function startDoubleMouseServer() {
    mouseServer = net.createServer(function(socket) {
        console.log('create socket server.');
        mouseSocket = socket;
        mouseSocket.composer = new Composer();

        mouseSocket.on('end', function() {
            console.log('socket end.');
        })

        mouseSocket.on('error', function() {
            console.log('socket error.');
        })

        mouseSocket.on('close', function() {
            console.log('socket close.');
        })

        mouseSocket.on('data', function(data) {
            mouseSocket.composer.feed(data);
        })

        mouseSocket.composer.on('data', function(data) {
            var pkg = JSON.parse(data.toString());
            if (pkg instanceof Array) {
                processMsgs(pkg);
            } else {
                processMsg(pkg);
            }
        })
    });

    mouseServer.on('error', function(err) {
        console.log('server error. ' + err);
    })

    mouseServer.on('close', function() {
        console.log('server close.');
    })

    mouseServer.on('connection', function() {
        console.log('server connection');
    })

    mouseServer.listen(listenPort, '0.0.0.0');
}

startDoubleMouseServer();

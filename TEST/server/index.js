const util = require('util');
const events = require('events');
const net = require('net');
const robot = require('robotjs');


const listenPort = 6231;
var mouseServer = null;
var mouseSocket = null;
var screensize = robot.getScreenSize();

function startDoubleMouseServer(){
	mouseServer = net.createServer(function(socket){
		console.log('create socket server.');
		mouseSocket = socket;

		mouseSocket.on('end', function(){
			console.log('socket end.');
		})

		mouseSocket.on('error', function(){
			console.log('socket error.');
		})

		mouseSocket.on('close', function(){
			console.log('socket close.');
		})

		mouseSocket.on('data', function(data){
			console.log('receive data ' + data);
			data = JSON.parse(data);
			if(data.type === 'click'){
				robot.mouseClick();
			}else if(data.type === 'move'){
				var x = Math.round((screensize.width * data.pos.x) /100);
				var y = Math.round((screensize.width * data.pos.y) /100);
				robot.moveMouse(x, y);
			}
		})
	});

	mouseServer.on('error', function(err){
		console.log('server error. ' + err);
	})

	mouseServer.on('close', function(){
		console.log('server close.');
	})

	mouseServer.on('connection', function(){
		console.log('server connection');
	})	

	mouseServer.listen(listenPort, 'localhost');
}

startDoubleMouseServer();
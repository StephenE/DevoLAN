// Global Variables
	var userToken = "";
	var screenTarget = "";
	var gamePulse = "";

	var apiUriBase = "http://devolan.azurewebsites.net/";
	var maxPlayers = 14;
	
	var currentGame = {};
	currentGame.GameId = "";
	currentGame.PhaseId = "";
	currentGame.PhaseType = 0;

	var currentPlayers = {};
	
	var system = {};
	system.music = 1;
	system.sfx = 1;
	system.musicVolume = 0.5;
	system.sfxVolume = 1;

// Load Screens
	function loadScreen(screen, noOverlay){
		console.log("Loading " + screen + "...");
		
		var url = "Content/parts/" + screen + ".html";
		sendAjax("GET", url, "", "form", displayScreen, displayScreen, false, "", true);
		
		screenTarget = screen;
		
		if(noOverlay === false){
			showOverlay("Loading...", "<img src='Content/images/waiting.svg' />");
		}
	}
	
	function displayScreen(){
		console.log(screenTarget + " loaded.");
		
		getID("canvas").innerHTML = this.responseText;
		loadScripts();
	}
	
	function loadScripts(){
		console.log("Loading scripts for " + screenTarget + "...");
		
		switch(screenTarget){
			case "login":
				hideOverlay();
				
				if(loadCookie("userToken")){
					userToken = loadCookie("userToken");
					loadScreen("browser");
				}
				
				addEvent("loginButton", "click", function(){loadAudio("sfx", "button"); login();}, false);
				addEvent("registerButton", "click", function(){loadAudio("sfx", "button"); register();}, false);
				break;
				
			case "browser":
				sessions();
				
				writeHTML("welcomeTitle", "Greetings, " + userToken.userName + ". Please select a game session to join.")
				
				addEvent("createButton", "click", function(){loadAudio("sfx", "button"); hostGame();}, false)
				addEvent("logoutButton", "click", function(){loadAudio("sfx", "confirm"); deleteCookie("userToken"); loadScreen("login");}, false)
				break;
				
			case "board":
				addClass("body", "bckWater");
				
				updateBoard();
				gamePulse = setInterval(updateBoard, 10000);
				
				setTimeout(function () { hideOverlay(); loadAudio("music", "planning"); }, 1500);

				window.addEventListener("resize", function () { resizeBoard() }, true);
			    resizeBoard();

			    addEvent("hudButtonEndTurn", "click", function () { loadAudio("sfx", "button"); endTurn(currentGame.GameId, currentGame.PhaseId); }, false)
				break;
		}
	}
	
// Login Screen
	// Login
		function login(loginUN, loginPW){
			// Validation
				if(typeof loginUN === "undefined"){
					loginUN = getValue("loginUsername");
					loginPW = getValue("loginPassword");
				}
				
				if(loginUN == ""){
					console.log("No username.");
					messageBox("Failed Validation.", "Please enter a username.");
					return false;
				}
				
				if(loginPW == "" || loginPW.length < 6){
					console.log("No password.");
					messageBox("Failed Validation.", "Please enter a valid password.");
					return false;
				}
			
			// Request
				var data = "grant_type=password&" + "username=" + loginUN + "&password=" + loginPW;
				sendAjax("POST", "/Token", "", "form", loginResponse, loginResponse, false, data);
				
				showOverlay("Logging in...", "<img src='Content/images/waiting.svg' />");
		}
		
		function loginResponse(){
			switch(this.status){
				case 200:
					console.log("Login successful.");
					
					userToken = JSON.parse(this.responseText);
					saveCookie("userToken", userToken);
					
					loadScreen("browser");
					break;
					
				default:
					console.log("Login failed.");
					
					messageBox("Login Failed.", "Please check your details and try again.");
					break;
			}
		}
		
	// Register
		function register(){
			// Get Values
				var regUN = getValue("registerUsername");
				var regPW = getValue("registerPassword");
				var regPWC = getValue("registerPasswordConfirm");
			
			// Validation
				if(regUN == ""){
					console.log("No username.");
					messageBox("Failed Validation.", "Please enter a username.");
					return false;
				}
				
				if(regPW == "" || regPW.length < 6){
					console.log("No password.");
					messageBox("Failed Validation.", "Please enter a password.");
					return false;
				}
				
				if(regPWC == "" || regPWC.length < 6 || regPW !== regPWC){
					console.log("No password confirmation.");
					messageBox("Failed Validation.", "Please enter a matching password.");
					return false;
				}
			
			// Request
				var data = '{"Email":"' + regUN + '", "Password":"' + regPW + '", "ConfirmPassword":"' + regPWC + '"}';
				sendAjax("POST", "/api/account/register", "", "json", regResponse, regResponse, false, data);
				
				showOverlay("Registering...", "<img src='Content/images/waiting.svg' />");
		}
		
		function regResponse(){
			switch(this.status){
				case 200:
					console.log("Registration successful.");
					
					showOverlay("Success!", "<p>Logging you in now...</p>");
					
					login(getValue("registerUsername"), getValue("registerPassword"));
					break;
					
				default:
					console.log("Registration failed.");
					
					messageBox("Registration Failed.", "Please check your details and try again.");
					break;
			}
		}
		
// Browser Screen
	// Retrieve Sessions
		function sessions(){
			console.log("Retrieving game sessions...");
			
			var data = "";
			sendAjax("GET", "/api/Game/Sessions", data, "adv", sesResponse, sesResponse, true);
			
			showOverlay("Finding Games...", "<img src='Content/images/waiting.svg' />");
		}
		
		function sesResponse(){
			console.log("Outputting game sessions...");
			
			var sessions = JSON.parse(this.responseText);
			var x = 0;
			var build = "";
			
			// Assemble Game Browser
				for(x = 0; x < sessions.length; x++){
					build += "<tr id='g-" + x + "' data-gameid='" + sessions[x].GameId + "' + class='hoverHighlight'>";
						build += "<td>" + sessions[x].GameId + "</td>";
						build += "<td>" + sessions[x].PhaseId + "</td>";
						
						build += "<td>";
							if(sessions[x].PhaseType == 0){
								build += "Open";
							} else if (sessions[x].PhaseType < 8) {
								build += "In Progress";
							} else {
								build += "Closed";
							}
						build += "</td>";
					build += "</tr>";
				}
				
				if(x === 0){
					build = "<td colspan='3'><h3>No game sessions found.</h3></td>";
				}
				
			// Output Game Browser
				writeHTML("browserBody", build);
				
			// Event Listeners
				var y = 0;
				
				for(y = 0; y < x; y++){
					addEvent("g-" + y, "click", function(){currentGame.GameId = getData(this.id, "gameid"); loadAudio("sfx", "button"); checkPlayers(currentGame.GameId);});
				}
			
			// Overlay
				hideOverlay();
		}
	
	// Check Players
		function checkPlayers(gameid){
			console.log("Checking game " + gameid + "...");
			
			var data = "?sessionId=" + gameid;
			updatePlayers(gameid, data, checkResponse, checkResponse);
			
			showOverlay("Checking Game...", "<img src='Content/images/waiting.svg' />");
		}
		
		function checkResponse(){
			console.log("Looking for player in game...");
			
			// Search for player in game
				var players = JSON.parse(this.responseText);
				var x = 0;
				var usedColours = [];
				
				currentPlayers = JSON.parse(this.responseText);
				
				for(x = 0; x < players.length; x++){
					usedColours[x] = players[x].Colour;
					
					if(players[x].Name === userToken.userName){
						console.log("Player is already part of game. Rejoining.");
						enterGame();
						return true;
					}
				}

				// Select Player Colour
					var content = "<div class='boxContainer'>";
						for(x = 0; x < maxPlayers; x++){
							if(usedColours.indexOf(x) < 0){
								content += "<div id='b-" + x + "' data-colour='" + x + "' class='box button player-" + x + "'><br/><br/></div>";
							}
						}

					content += "</div>";
					content += "<input id='b-cancel' type='button' value='Cancel' />";
					
					showOverlay("Select Your Player Colour.", content);
					
				// Add Event Listeners
					addEvent("b-cancel", "click", function(){hideOverlay();});
					for(x = 0; x < maxPlayers; x++){
						if(usedColours.indexOf(x) < 0){
							addEvent("b-" + x, "click", function(){loadAudio("sfx", "confirm"); joinGame(currentGame.GameId, getData(this.id, "colour"));});
						}
					}
		}
	
	// Join Game
		function joinGame(gameid, colour){
			console.log("Joining game " + gameid + " as " + colour + "...");
			
			var data = "?sessionId=" + gameid + "&colour=" + colour;
			sendAjax("POST", "/api/Game/JoinGame", data, "adv", joinResponse, joinResponse, true);
				
			showOverlay("Joining Game...", "<img src='Content/images/waiting.svg' />");
		}
		
		function joinResponse(){
			switch(this.status){
				case 204:
					console.log("Join request successful.");
					
					enterGame();
					break;
					
				default:
					console.log("Join request failed.");
					
					messageBox("Join failed.", "Unable to join game.");
					break;
			}
		}
		
	// Enter Game
		function enterGame(){
			console.log("Entering game...")
			
			fadeAudio("music", 0, 1.5);
			showOverlay("Entering Game...", "<img src='Content/images/waiting.svg' />", true);
			
			var timer = setTimeout(function(){loadScreen("board", true);}, 1500);
		}
		
	// Host Game
		function hostGame(){
		    console.log("Creating game...");
		    var data = "?colour=1";
		    sendAjax("POST", "api/Game/StartNewGame", data, "json", onHostGameResponse, onHostGameResponse, true);
		}

		function onHostGameResponse() {
            // TODO: Join game!
		}
		
// Game Board
	// Update Board
		function updateBoard(){
			console.log("Getting current game board status...");
			
			var data = "?sessionId=" + currentGame.GameId;

			updateGameState(currentGame.GameId);

			if (currentGame.PhaseType === 0) {
			    updatePlayers(currentGame.GameId, data, playersResponse, playersResponse);
			}

			sendAjax("GET", "api/World/RegionList", data, "adv", updateResponse, updateResponse, true);
		}
		
		function updateResponse(){
			console.log("Updating board to current state...");
			
			var world = JSON.parse(this.responseText);
			var x = 0;
			
			for(x = 0; x < world.length; x++){
				var target = "territory" + world[x].Name.replace(/ /g, "");
				var y = 0;
				var cpLength = currentPlayers.length;
				var curColour = "";
				
				for(y = 0; y < cpLength; y++){
					if(world[x].OwnerId === currentPlayers[y].UserId){
						curColour = currentPlayers[y].Colour;
			        }
		        }
				
				addClass(target, "player-" + curColour);
				setTextContent(target + "-counter", world[x].TroopCount);

				setData(target, "RegionId", world[x].RegionId);
				setData(target, "ContinentId", world[x].ContinentId);
				setData(target, "OwnerId", world[x].OwnerId);
				setData(target, "TroopCount", world[x].TroopCount);
			}
		}
		
	// Update Player Information
		function updatePlayers(gameid, data, eventDone, eventError){
			console.log("Getting player information for game " + gameid + ".");
			
			sendAjax("GET", "/api/Game/Players", data, "adv", eventDone, eventError, true);
		}
		
		function playersResponse(){
			console.log("Updating player information...");
			
			currentPlayers = JSON.parse(this.responseText);
		}

    // Update Phase Information
		function updateGameState(gameid) {
		    console.log("Getting game state information...");

		    var data = "?sessionId=" + currentGame.GameId;
            sendAjax("GET", "api/Game/Session", data, "adv", gameStateResponse, gameStateResponse, true)
		}

		function gameStateResponse() {
		    console.log("Updating current game state.");

		    var gamestate = JSON.parse(this.responseText);

            // PhaseId doesn't change if the type doesn't, but we need to get it at least once
		    currentGame.PhaseId = gamestate.PhaseId;
		    if (gamestate.PhaseType !== currentGame.PhaseType){
		        currentGame.PhaseType = gamestate.PhaseType;

		        showPhase(currentGame.PhaseType);
		    }
		}

    // Game Phase Controller
		function showPhase(phaseType) {
            console.log("Entering phase " + phaseType + ".")

		    var instruction = "";

		    switch (currentGame.PhaseType) {
		        case 0:
		            instruction = "Waiting for host to begin the game...";
		            break;

		        case 1:
		            instruction = "Reinforcements Phase: Deploy reinforcements."
		            reinforcementsPhase(currentGame.GameId);
		            break;

		        case 2:
		            instruction = "Combat Orders: Assign combat orders."
		            break;

		        case 3:
		            instruction = "Combat Phase: Showing border clashes.";
		            break;

		        case 4:
		            instruction = "Combat Phase: Showing mass invasions.";
		            break;

		        case 5:
		            instruction = "Combat Phase: Showing invasions.";
		            break;

		        case 6:
		            instruction = "Combat Phase: Showing spoils of war";
		            break;

		        case 7:
		            instruction = "Redeployment Phase: Redeploy your troops.";
		            break;
		    }

		    writeHTML("hudTextInstructions", instruction);
		}

// Game Phases
    // Reinforcements
		function reinforcementsPhase(gameid){
		    console.log("Getting reinforcements.");

		    var data = "?sessionId=" + currentGame.GameId;
		    sendAjax("GET", "api/Nation/Reinforcements", data, "adv", reinforcementsResponse, null, true)
		}

		function reinforcementsResponse(){

		    return this.responseText;
		}

    // Combat
		function orderAttack(gameId, sourceRegionId, numberOfTroops, targetRegionId) {
		    console.log("Attacking from " + sourceRegionId + " to " + targetRegionId);

		    var data = "?sessionId=" + gameId + "&regionId=" + sourceRegionId + "&numberOfTroops=" + numberOfTroops + "&targetRegionId=" + targetRegionId;
		    sendAjax("POST", "api/Region/Attack", data, "json", onOrderAttackResponse, onOrderAttackResponse, true);
		}

		function onOrderAttackResponse() {
		    switch(this.status){
		        case 204:
		            console.log("Attack order successful");
                    // TODO: Visual Feedback?
		            break;
		        case 400:
		            console.log("Attack failed. Troops already committed");
		            messageBox("Attack failed.", "There were not enough idle troops to attack with");
		            break;
		        case 402:
		            console.log("Attack failed. Regions not connected");
		            messageBox("Attack failed.", "Source and target region are not connected");
		            break;
		        case 404:
		            console.log("Attack failed. Invalid region id");
		            messageBox("Attack failed.", "Invalid region");
		            break;
		        case 406:
		            console.log("Attack failed. Not owner of source, or owner of target");
		            messageBox("Attack failed.", "Not owner of source region, or you are the owner of the target region!");
		            break;
		        case 417:
		            console.log("Attack failed. Invalid session phase");
		            messageBox("Attack failed.", "The combat orders phase is over");
		            break;
		        default:
		            console.log("Join request failed.");
		            messageBox("Attack failed.", "Not sure why -  Error code" + this.status);
		            break;
		    }
        }
		
	// Hud
		function showHud() {

		}

		function hideHud() {

		}

		function endTurn(gameId, phaseId) {
		    // Technically, only the session host can do this. Anyone else will get an error and be ignored - they should EndPhase, but for DevoLAN 31 that doesn't actual do anything!
			var data = "?sessionId=" + gameId + "&phaseId=" + phaseId + "&force=true";
			sendAjax("POST", "/api/Game/AdvanceNextPhase", data, "json", onEndTurnResponse, onEndTurnResponse, true);
		}

		function onEndTurnResponse() {
			// No response expected.
		}
// Message Box
	function messageBox(title, message){
		hideOverlay();
		
		writeHTML("message", "<h3>" + title + "</h3><p>" + message + "</p>");
		addClass("message", "show");
	}
		
// Overlay
	function showOverlay(title, content, blackout){
		//console.log("Showing overlay.");
		
		writeHTML("overlayContent", "<h1>" + title + "</h1>" + content);
		addClass("overlay", "show");
		
		if(blackout){
			addClass("overlay", "blackout", 1);
		}
	}
	
	function hideOverlay(fade){
		//console.log("Hiding overlay.");
		
		if(fade){
			removeClass("overlay", "blackout");
			setTimeout(hideOverlay, 1500);
		} else {
			removeClass("overlay", "show");
		}
	}
	
// Save User Login Session
	function saveCookie(name, data){
		//console.log("Saving " + name + " cookie.");
		
		document.cookie = name + "=" + JSON.stringify(data);
	}
	
	function loadCookie(name){
		var cookies = document.cookie.split(";");
		var x = 0;
		
		//console.log("Checking for " + name + " cookie...");
		
		// Cycle Cookies
			for(x = 0; x < cookies.length; x++){
				if(cookies[x].indexOf(name) > 0){
					console.log(name + " cookie found.");
					
					return JSON.parse(cookies[x].substring(cookies[x].indexOf("{"), cookies[x].length));
				}
			}
			
		console.log(name + " cookie not found.");
		return false;
	}
	
	function deleteCookie(name){
		//console.log("Removing " + name + " cookie.")
		
		document.cookie = name + "=''; expires=Thu, 01 Jan 1970 00:00:00 UTC";
	}
	
// Menu & Settings
	// Menu
		function menuRefresh(){
			//console.log("Building settings menu.");
			
			var build = "";
			
			// Music Button
				build += "<img id='buttonMusic' class='menuButton' src='Content/images/music";
					if(system.music == 0){
						build += "Off";
					} else {
						build += "On";
					}
				build += ".png' title='Toggle Music' />";
				
			// SFX Button
				build += "<img id='buttonSFX' class='menuButton' src='Content/images/sfx";
					if(system.sfx == 0){
						build += "Off";
					} else {
						build += "On";
					}
				build += ".png' title='Toggle SFX' />";
				
			// Output Buttons
				writeHTML("menu", build);
			
			// Event Listeners
				addEvent("buttonMusic", "click", function(){toggleMusic();}, false);
				addEvent("buttonSFX", "click", function(){toggleSFX();}, false);
				
			// Update Cookies
				saveCookie("system", system);
		}
		
	// Toggle Music
		function toggleMusic(){
			if(system.music == 0){
				system.music = 1;
				playAudio("music");
			} else {
				system.music = 0;
				stopAudio("music");
			}
			
			menuRefresh();
		}
		
	// Toggle SFX
		function toggleSFX(){
			if(system.sfx == 0){
				system.sfx = 1;
				playAudio("sfx");
			} else {
				system.sfx = 0;
				stopAudio("sfx");
			}
			
			menuRefresh();
		}
		
// Audio Handling
	function playAudio(target){
		console.log("Playing " + target + ".");
		
		var player = getID(target);
		
		switch(target){
			case "sfx":
				if(system.sfx){
					player.play();
				}
				break;
				
			case "music":
				if(system.music){
					player.play();
				}
				break;
		}
	}
	
	function loadAudio(target, sound){
		console.log("Loading " + sound + " on " + target + " channel.");
		
		var source = getID(target + "Source");
		
		stopAudio(target);
		source.src = "Content/media/" + sound + ".mp3";
		getID(target).load();
		
		playAudio(target);
	}
		
	function stopAudio(target){
		console.log("Stopping " + target + ".");
		
		getID(target).pause();
	}
	
	function setAudioVolume(target, level){
		//console.log("Adjusting " + target + " to volume level " + level + ".");
		
		var player = getID(target);
		player.volume = level;
	}
	
	function fadeAudio(target, level, duration){
		//console.log("Fading " + target + " to " + level + " over " + duration + " seconds.");
		
	    var player = getID(target);
	    var initial = player.volume;

		duration = (duration * 1000) / (duration * 25);
		level = initial / duration;
		
		var timer = setInterval(function(){
				try{
					player.volume -= level;
				}catch(err){
					player.volume = 0;
					clearInterval(timer);
				}

				if (player.volume === 0.0) {
				    stopAudio(target);
                    setAudioVolume(target, initial)
					clearInterval(timer);
				}
			}, duration);
	}
	
// Screen Sizing
	function resizeBoard(){
			var x = 0;
			var circles = document.getElementsByTagName("circle");
			var r = 17.5;
				
			for(x = 0; x < circles.length; x++){
				if(window.innerWidth < 1100 || window.innerHeight < 650){
					r = 35;
				} else {
					r = 17.5;
				}
					
				circles[x].setAttribute("r", r);
			}
	}
		
// Fill
	// Set Fill Colour
		function setFillColour(target, colour){
			getID(target).style.fill = colour;
		}
	
// Stroke
	// Set Stroke Colour
		function setStrokeColour(target, colour){
			getID(target).style.stroke = colour;
		}
		
	// Set Stroke Width
		function setStrokeWeight(target, weight){
			getID(target).style['stroke-width'] = weight;
		}
		
// Text
	// Set Text Value
		function setTextValue(target, value){
			getID(target).textContent = value;
		}
		
// AJAX Requests
	function sendAjax(method, url, urlData, content, eventLoad, eventError, auth, sendData, noURI){
		var ajaxRequest = new XMLHttpRequest();
		
		if (noURI) {
		    ajaxRequest.open(method, url + urlData);
		} else {
		    ajaxRequest.open(method, apiUriBase + url + urlData);
		}
		
		if (eventLoad) {
		    ajaxRequest.addEventListener("load", eventLoad);
		}

		if (eventError) {
		    ajaxRequest.addEventListener("error", eventError);
		}
			
		if(auth){
			ajaxRequest.setRequestHeader("Accept", "text/html");
			ajaxRequest.setRequestHeader("Authorization", "bearer " + userToken.access_token);
		}
		
		switch(content){
			case "form":
				ajaxRequest.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
				break;
				
			case "json":
				ajaxRequest.setRequestHeader("Content-Type", "application/json; charset=UTF-8");
				break;
			
			case "adv":
			default:
				ajaxRequest.setRequestHeader("Content-Type", "application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
				break;
		}
		
		ajaxRequest.send(sendData);
		
		return ajaxRequest;
	}

// DOM Requests
	// Classes
		function addClass(target, clss){
			getID(target).classList.add(clss)
		}
		
		function removeClass(target, clss){
			//console.log("Removing " + clss + " from " + target + ".");
			
			if(checkClass(target, clss)){
				getID(target).classList.remove(clss);
			}
		}
		
		function toggleClass(target, clss){
			getID(target).classList.toggle(clss);
			}
			
		function checkClass(target, clss){
			return getID(target).classList.contains(clss);
		}
		
	// Data
		function getData(target, data){
			return getID(target).getAttribute("data-" + data);
		}
		
		function setData(target, data, value){
			getID(target).setAttribute("data-" + data, value);
		}
		
	// Get Element by ID
		function getID(target){
			return document.getElementById(target);
		}

	// Styles
		function writeStyle(t, v){
			document.getElementById(t).setAttribute("style", v);
				
			return;
		}
		
	// Event Listeners
		function addEvent(target, evnt, func, c){
			getID(target).addEventListener(evnt, func, c);
		}
		
	// innerHTML
		function writeHTML(target, value, append){
			if(append === true){
				document.getElementById(target).innerHTML += value;
			} else {
				document.getElementById(target).innerHTML = value;
			}
		}

    // Text Content
		function setTextContent(target, text) {
		    getID(target).textContent = text;
		}
	
	// Values
		function getValue(target){
			return getID(target).value;
		}
// Global Variables
	var userToken = "";
	var screenTarget = "";
	
	var system = {};
	system.music = 1;
	system.sfx = 1;

// Load Screens
	function loadScreen(screen){
		console.log("Loading " + screen + "...")
		
		var loadRequest = new XMLHttpRequest();
		var loadTarget = "Content/parts/" + screen + ".html";
		
		loadRequest.addEventListener("load", displayScreen)
		loadRequest.open("GET", loadTarget)
		loadRequest.send();
		
		screenTarget = screen;
		
		showOverlay("Loading...", "<img src='Content/images/waiting.svg' />");
	}
	
	function displayScreen(){
		console.log(screenTarget + " loaded.");
		
		getID("canvas").innerHTML = this.responseText;
		hideOverlay();
		
		loadScripts();
	}
	
	function loadScripts(){
		console.log("Loading scripts for " + screenTarget + "...");
		
		switch(screenTarget){
			case "login":
				if(loadCookie("userToken")){
					userToken = loadCookie("userToken");
					loadScreen("browser");
				}
				
				addEvent("loginButton", "click", function(){playAudio("sfx", "button"); login();}, false);
				addEvent("registerButton", "click", function(){playAudio("sfx", "button"); register();}, false);
				break;
				
			 case "browser":
				sessions();
				
				addEvent("createButton", "click", function(){playAudio("sfx", "button"); hostGame();}, false)
				addEvent("logoutButton", "click", function(){playAudio("sfx", "confirm"); deleteCookie("userToken"); loadScreen("login");}, false)
				break;
		}
	}
	
// Login Screen
	// Login
		function login(loginUN, loginPW){
			if(typeof loginUN === "undefined"){
				loginUN = getValue("loginUsername");
				loginPW = getValue("loginPassword");
			}
			
			if(loginUN == ""){
				console.log("No username.");
				loginError("Failed Validation.", "Please enter a username.");
				return false;
			}
			
			if(loginPW == "" || loginPW.length < 6){
				console.log("No password.");
				loginError("Failed Validation.", "Please enter a valid password.");
				return false;
			}
			
			// Format Request
				var loginData = "grant_type=password&" + "username=" + loginUN + "&password=" + loginPW;
			
			// Send Login Requests
				var loginRequest = new XMLHttpRequest();
				
				loginRequest.addEventListener("load", loginResponse);
				loginRequest.addEventListener("error", loginResponse);
				
				loginRequest.open("POST", "http://devolan.azurewebsites.net/Token");
				loginRequest.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
				loginRequest.send(loginData);
				
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
					
					loginError("Login Failed.", "Please check your details and try again.");
					break;
			}
		}
		
		function loginError(title, message){
			hideOverlay();
			
			writeHTML("message", "<h3>" + title + "</h3><p>" + message + "</p>");
			writeClass("message", "show");
		}
		
	// Register
		function register(){
			var regUN = getValue("registerUsername");
			var regPW = getValue("registerPassword");
			var regPWC = getValue("registerPasswordConfirm");
			
			if(regUN == ""){
				console.log("No username.");
				loginError("Failed Validation.", "Please enter a username.");
				return false;
			}
			
			if(regPW == "" || regPW.length < 6){
				console.log("No password.");
				loginError("Failed Validation.", "Please enter a password.");
				return false;
			}
			
			if(regPWC == "" || regPWC.length < 6 || regPW !== regPWC){
				console.log("No password confirmation.");
				loginError("Failed Validation.", "Please enter a matching password.");
				return false;
			}
				
			// Format Request
				var regData = '{"Email":"' + regUN + '", "Password":"' + regPW + '", "ConfirmPassword":"' + regPWC + '"}';
			
			// Send Registration Request
				var regRequest = new XMLHttpRequest();
				
				regRequest.addEventListener("load", regResponse);
				regRequest.addEventListener("error", regResponse);
				
				regRequest.open("POST", "http://devolan.azurewebsites.net/api/account/register");
				regRequest.setRequestHeader("Content-Type", "application/json; charset=UTF-8");
				regRequest.send(regData);
				
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
					
					loginError("Registration Failed.", "Please check your details and try again.");
					break;
			}
		}
		
// Browser Screen
	// Retrieve Sessions
		function sessions(){
			console.log("Retrieving game sessions...");
			
			// Send Session Request
				var sesRequest = new XMLHttpRequest();
				
				sesRequest.addEventListener("load", sesResponse);
				sesRequest.addEventListener("error", sesResponse);
				
				sesRequest.open("GET", "http://devolan.azurewebsites.net/api/Game/Sessions");
				sesRequest.setRequestHeader("Accept", "text/html");
				sesRequest.setRequestHeader("Authorization", "bearer " + userToken.access_token);
				sesRequest.setRequestHeader("Content-Type", "application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
				
				sesRequest.send();
				
				showOverlay("Finding Games...", "<img src='Content/images/waiting.svg' />");
		}
		
		function sesResponse(){
			console.log("Outputting game sessions...");
			
			var sessions = JSON.parse(this.responseText);
			var x = 0;
			var build = "";
			
			// Assemble Game Browser
				for(x = 0; x < sessions.length; x++){
					build += "<tr>";
						build += "<td>" + sessions[x].GameId + "</td>";
						build += "<td>" + sessions[x].PhaseId + "</td>";
						
						build += "<td>";
							if(sessions[x].PhaseType == 0){
								build += "Open";
							} else if(session[x].PhaseType < 8){
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
				hideOverlay();
		}
		
	// Host Game
		function hostGame(){
			console.log("Creating game...")
		}
		
// Board
	// Setup Board
		function boardSetup(){
			// Resize Listener
				window.addEventListener("resize", function(){resizeBoard()}, true);
				resizeBoard();
		}
		
// Overlay
	function showOverlay(title, content){
		console.log("Showing overlay.");
		
		writeHTML("overlayContent", "<h1>" + title + "</h1>" + content);
		writeClass("overlay", "show");
	}
	
	function hideOverlay(){
		console.log("Hiding overlay.");
		
		writeClass("overlay", "hide");
	}
	
// Save User Login Session
	function saveCookie(name, data){
		console.log("Saving " + name + " cookie...");
		
		document.cookie = name + "=" + JSON.stringify(data);
		
		console.log(name + " cookie saved.");
	}
	
	function loadCookie(name){
		var cookies = document.cookie.split(";");
		var x = 0;
		
		console.log("Checking for " + name + " cookie...");
		
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
		document.cookie = name + "=''; expires=Thu, 01 Jan 1970 00:00:00 UTC";
	}
	
// Menu & Settings
	// Menu
		function menuRefresh(){
			console.log("Building settings menu.");
			
			var build = "";
			
			// Music Button
				build += "<img id='buttonMusic' class='button' src='Content/images/music";
					if(system.music == 0){
						build += "Off";
					} else {
						build += "On";
					}
				build += ".png' title='Toggle Music' />";
				
			// SFX Button
				build += "<img id='buttonSFX' class='button' src='Content/images/sfx";
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
				getID("music").volume = 0.5;
			} else {
				system.music = 0;
				getID("music").volume = 0;
			}
			
			menuRefresh();
		}
		
	// Toggle SFX
		function toggleSFX(){
			if(system.sfx == 0){
				system.sfx = 1;
				getID("sfx").volume = 1;
			} else {
				system.sfx = 0;
				getID("sfx").volume = 0;
			}
			
			menuRefresh();
		}
		
// Audio Handling
	function playAudio(target, sound){
		console.log("Changing " + target + " to " + sound + ".");
		
		var player = getID(target);
		var source = getID(target + "Source");
		
		player.pause();
		source.src = "Content/media/" + sound + ".mp3";
		player.load();
		player.play();
	}
	
// Screen Sizing
	// Circle Sizer
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
		
// DOM Requests
	// Write Classes
		function writeClass(t, v, m){
			if(m == 1){
				document.getElementById(t).className += v;
			} else {
				document.getElementById(t).className = v;
			}
			
			return;
		}
		
	// Get Element by ID
		function getID(target){
			return document.getElementById(target);
		}

	// Element Styles
		// Reset Series of Styles
			function resetStyles(prefix, max, base){
				try{
					var x = 0;
					
					for(x = 0; x < max; x++){
						writeClass(prefix + x, base);
					}
					
				} catch(err){
					console.log("Done " + x);
					
				}
			}
			
		// Write Styles
			function writeStyle(t, v){
				document.getElementById(t).setAttribute("style", v);
				
				return;
			}
		
	// Add Event Listener
		function addEvent(target, ev, func, c){
			getID(target).addEventListener(ev, func, c);
		}
		
	// Write innerHTML
		function writeHTML(t, v, m){
			if(m == 1){
				document.getElementById(t).innerHTML += v;
			} else {
				document.getElementById(t).innerHTML = v;
			}
		}
	
	// Get Value
		function getValue(target){
			return getID(target).value;
		}
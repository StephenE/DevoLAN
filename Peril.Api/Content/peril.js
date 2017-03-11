// Global Variables
var userToken = "";
var screenTarget = "";
var gamePulse = "";
var interactionTarget = ";"
var firstRun = true;

var apiUriBase = window.location.protocol + "//" + window.location.host + "/";
var maxPlayers = 14;

var currentGame = {};
currentGame.GameId = "";
currentGame.PhaseId = "";
currentGame.PhaseType = null;
currentGame.OwnerId = "";

var currentPlayers = {};

var currentPhase = {};
currentPhase.reinforcements = 0;

var worldLookup = {};

var system = {};
system.music = 1;
system.sfx = 1;
system.musicVolume = 0.5;
system.sfxVolume = 1;

// Load Screens
    function loadScreen(screen, noOverlay)
    {
        console.log("Loading " + screen + "...");
        
        var url = "Content/parts/" + screen + ".html";
        sendAjax("GET", url, "", "form", displayScreen, displayScreen, false, "", true);
        
        screenTarget = screen;
        
        if (noOverlay === false)
        {
            showOverlay("Loading...", "<img src='Content/images/waiting.svg' />");
        }
    }
    
    function displayScreen()
    {
        console.log(screenTarget + " loaded.");
        
        getID("canvas").innerHTML = this.responseText;
        loadScripts();
    }
    
    function loadScripts()
    {
        console.log("Loading scripts for " + screenTarget + "...");
        
        switch (screenTarget)
        {
            case "login":
                hideOverlay();
                
                if (loadCookie("userToken"))
                {
                    userToken = loadCookie("userToken");
                    loadScreen("browser");
                }
                
                addEvent("loginButton", "click", function(){loadAudio("sfx", "button"); login();}, false);
                addEvent("registerButton", "click", function(){loadAudio("sfx", "button"); register();}, false);
                break;
                
            case "browser":
                sessions();
                
                writeHTML("welcomeTitle", "Greetings, " + userToken.userName + ". Please select a game session to join.")
                
                addEvent("createButton", "click", function () { loadAudio("sfx", "button"); colourSelectionScreen("host"); }, false)
                addEvent("logoutButton", "click", function(){loadAudio("sfx", "confirm"); deleteCookie("userToken"); loadScreen("login");}, false)
                break;
                
            case "board":
                addClass("body", "bckWater");
                
                updateGameState();
                gamePulse = setInterval(updateGameState, 2500);
                
                setTimeout(function () { hideOverlay(true); loadAudio("music", "planning"); }, 1500);

                window.addEventListener("resize", function () { resizeBoard() }, true);
                resizeBoard();

                addEvent("hudButtonEndTurn", "click", function () { loadAudio("sfx", "button"); endTurn(currentGame.GameId, currentGame.PhaseId); }, false)
                break;
        }
    }
    
// Login Screen
    // Login
        function login(loginUN, loginPW)
        {
            // Validation
            if (typeof loginUN === "undefined")
            {
                loginUN = getValue("loginUsername");
                loginPW = getValue("loginPassword");
            }
                
            if (loginUN == "")
            {
                console.log("No username.");
                messageBox("Failed Validation.", "Please enter a username.");
                return false;
            }
                
            if (loginPW == "" || loginPW.length < 6)
            {
                console.log("No password.");
                messageBox("Failed Validation.", "Please enter a valid password.");
                return false;
            }
            
            // Request
            var data = "grant_type=password&" + "username=" + loginUN + "&password=" + loginPW;
            sendAjax("POST", "/Token", "", "form", loginResponse, loginResponse, false, data);
                
            showOverlay("Logging in...", "<img src='Content/images/waiting.svg' />");
        }
        
        function loginResponse()
        {
            switch (this.status)
            {
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
        function register()
        {
            // Get Values
            var regUN = getValue("registerUsername");
            var regPW = getValue("registerPassword");
            var regPWC = getValue("registerPasswordConfirm");
            
            // Validation
            if (regUN == "")
            {
                console.log("No username.");
                messageBox("Failed Validation.", "Please enter a username.");
                return false;
            }
                
            if (regPW == "" || regPW.length < 6)
            {
                console.log("No password.");
                messageBox("Failed Validation.", "Please enter a password.");
                return false;
            }
                
            if (regPWC == "" || regPWC.length < 6 || regPW !== regPWC)
            {
                console.log("No password confirmation.");
                messageBox("Failed Validation.", "Please enter a matching password.");
                return false;
            }
            
            // Request
            var data = '{"Email":"' + regUN + '", "Password":"' + regPW + '", "ConfirmPassword":"' + regPWC + '"}';
            sendAjax("POST", "/api/account/register", "", "json", regResponse, regResponse, false, data);
                
            showOverlay("Registering...", "<img src='Content/images/waiting.svg' />");
        }
        
        function regResponse()
        {
            switch (this.status)
            {
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
        function sessions()
        {
            console.log("Retrieving game sessions...");
            
            var data = "";
            sendAjax("GET", "/api/Game/Sessions", data, "adv", sesResponse, sesResponse, true);
            
            showOverlay("Finding Games...", "<img src='Content/images/waiting.svg' />");
        }
        
        function sesResponse()
        {
            console.log("Outputting game sessions...");
            
            var sessions = JSON.parse(this.responseText);
            var x = 0;
            var build = "";
            
            // Assemble Game Browser
            for (x = 0; x < sessions.length; x++)
            {
                build += "<tr id='g-" + x + "' data-gameid='" + sessions[x].GameId + "' + class='hoverHighlight'>";
                build += "<td>" + sessions[x].GameId + "</td>";
                build += "<td>" + sessions[x].PhaseId + "</td>";
                        
                build += "<td>";
                if (sessions[x].PhaseType == 0)
                {
                    build += "Open";
                }
                else if (sessions[x].PhaseType < 8)
                {
                    build += "In Progress";
                }
                else
                {
                    build += "Closed";
                }
                build += "</td>";
                build += "</tr>";
            }

            if (x === 0)
            {
                build = "<td colspan='3'><h3>No game sessions found.</h3></td>";
            }
                
            // Output Game Browser
            writeHTML("browserBody", build);
                
            // Event Listeners
            var y = 0;
                
            for (y = 0; y < x; y++)
            {
                addEvent("g-" + y, "click", function(){currentGame.GameId = getData(this.id, "gameid"); loadAudio("sfx", "button"); checkPlayers(currentGame.GameId);});
            }
            
            // Overlay
                hideOverlay();
        }
    
    // Check Players
        function checkPlayers(gameid){
            console.log("Checking game " + gameid + "...");
            
            updatePlayers(gameid, checkResponse, checkResponse);
            
            showOverlay("Checking Game...", "<img src='Content/images/waiting.svg' />");
        }
        
        function checkResponse()
        {
            console.log("Looking for player in game...");
            
            // Search for player in game
            var players = JSON.parse(this.responseText);
            var x = 0;
            var usedColours = [];
                
            currentPlayers = JSON.parse(this.responseText);
                
            for (x = 0; x < players.length; x++)
            {
                usedColours[x] = players[x].Colour;
                    
                if (players[x].Name === userToken.userName)
                {
                    console.log("Player is already part of game. Rejoining.");
                    enterGame();
                    return true;
                }
            }

            colourSelectionScreen("join", usedColours);
        }

        function colourSelectionScreen(mode, usedColours)
        {
            if (mode === "host")
            {
                usedColours = [];
            }

            var x = 0;
            var content = "<div class='boxContainer'>";
            for (x = 0; x < maxPlayers; x++)
            {
                if (usedColours.indexOf(x) < 0)
                {
                    content += "<div id='b-" + x + "' data-colour='" + x + "' class='box button player-" + x + "'><br/><br/></div>";
                }
            }

            content += "</div>";
            content += "<input id='b-cancel' type='button' value='Cancel' />";

            showOverlay("Select Your Player Colour.", content);

            addEvent("b-cancel", "click", function () { hideOverlay(); });

            for (x = 0; x < maxPlayers; x++)
            {
                if (usedColours.indexOf(x) < 0)
                {
                    switch (mode)
                    {
                        case "join":
                            addEvent("b-" + x, "click", function () { loadAudio("sfx", "confirm"); joinGame(currentGame.GameId, getData(this.id, "colour")); });
                            break;

                        case "host":
                            addEvent("b-" + x, "click", function () { loadAudio("sfx", "confirm"); hostGame(getData(this.id, "colour")); });
                            break;
                    }
                }
            }
        }
    
    // Join Game
        function joinGame(gameid, colour)
        {
            console.log("Joining game " + gameid + " as " + colour + "...");
            
            var data = "?sessionId=" + gameid + "&colour=" + colour;
            sendAjax("POST", "/api/Game/JoinGame", data, "adv", joinResponse, joinResponse, true);
                
            showOverlay("Joining Game...", "<img src='Content/images/waiting.svg' />");
        }
        
        function joinResponse()
        {
            switch (this.status)
            {
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
        function enterGame()
        {
            console.log("Entering game...")
            
            firstRun = true;
            fadeAudio("music", 0, 1.5);
            showOverlay("Entering Game...", "<img src='Content/images/waiting.svg' />", true);
            
            var timer = setTimeout(function(){loadScreen("board", true);}, 1500);
        }
        
    // Host Game
        function hostGame(colour)
        {
            console.log("Creating game...");
            var data = "?colour=" + colour;
            sendAjax("POST", "api/Game/StartNewGame", data, "json", onHostGameResponse, onHostGameResponse, true);

            showOverlay("Creating Game...", "<img src='Content/images/waiting.svg' />");
        }

        function onHostGameResponse()
        {
            console.log("New game created.")
            var session = JSON.parse(this.responseText);

            currentGame.GameId = session.GameId;
            checkPlayers(currentGame.GameId);
        }
        
// Game Board
    // Update Board
        function updateBoard()
        {
            console.log("Getting current game board status...");
            
            var data = "?sessionId=" + currentGame.GameId;
            sendAjax("GET", "api/World/RegionList", data, "adv", updateResponse, updateResponse, true);
        }
        
        function updateResponse()
        {
            console.log("Updating board to current state...");

            var world = JSON.parse(this.responseText);
            var x = 0;

            for (x = 0; x < world.length; x++)
            {
                processRegionData(world[x]);
            }

            updateCombat();
        }

        function processRegionData(regionData)
        {
            var target = "territory" + regionData.Name.replace(/ /g, "");
            var y = 0;
            var cpLength = currentPlayers.length;
            var curColour = "";

            console.log("Next territory to check for owner: " + regionData.Name);

            for (y = 0; y < cpLength; y++) {
                if (regionData.OwnerId === currentPlayers[y].UserId) {
                    curColour = currentPlayers[y].Colour;
                    break;
                }
            }

            replaceClass(target + "-circle", "player-" + curColour);
            setTextContent(target + "-counter", regionData.TroopCount);

            var targetElement = getID(target);
            setDataOnElement(targetElement, "FriendlyName", regionData.Name);
            setDataOnElement(targetElement, "OwnerId", regionData.OwnerId);
            setDataOnElement(targetElement, "RegionId", regionData.RegionId);
            setDataOnElement(targetElement, "ContinentId", regionData.ContinentId);
            setDataOnElement(targetElement, "TroopCount", regionData.TroopCount);
            setDataOnElement(targetElement, "ConnectedRegions", regionData.ConnectedRegions);

            worldLookup[regionData.RegionId] = regionData.Name;

            addEvent(target, "click", function () { territoryInteraction(targetElement); }, false);
            addEvent(target + "-outline", "click", function () { territoryInteraction(targetElement); }, false);
            removeAllAttacks(targetElement);
        }

        function updateCombat()
        {
            console.log("Getting current combat status...");

            var data = "?sessionId=" + currentGame.GameId;
            sendAjax("GET", "api/World/Combat", data, "adv", updateCombatResponse, updateCombatResponse, true);
        }

        function updateCombatResponse()
        {
            console.log("Updating combat on board to current state...");

            var plannedCombat = JSON.parse(this.responseText);
            for (var counter = 0; counter < plannedCombat.length; counter++)
            {
                processCombatEntry(plannedCombat[counter]);
            }
        }

        function processCombatEntry(combatEntry)
        {
            console.log("Processing combat " + combatEntry.CombatId + " of type " + combatEntry.ResolutionType);
            if(combatEntry.ResolutionType === 0)
            {
                // Border clash
                var armyA = combatEntry.InvolvedArmies[0];
                var armyB = combatEntry.InvolvedArmies[1];

                createOrUpdateAttack(armyA.OriginRegionId, armyB.OriginRegionId, armyA.NumberOfTroops, false);
                createOrUpdateAttack(armyB.OriginRegionId, armyA.OriginRegionId, armyB.NumberOfTroops, false);
            }
            else
            {
                // All other combat. One army defending, the others are all attackers
                var defendingRegionId;
                for (var counter = 0; counter < combatEntry.InvolvedArmies.length; counter++)
                {
                    if(combatEntry.InvolvedArmies[counter].ArmyMode === 1)
                    {
                        defendingRegionId = combatEntry.InvolvedArmies[counter].OriginRegionId;
                        break;
                    }
                }

                for (counter = 0; counter < combatEntry.InvolvedArmies.length; counter++)
                {
                    if (combatEntry.InvolvedArmies[counter].ArmyMode === 0)
                    {
                        createOrUpdateAttack(combatEntry.InvolvedArmies[counter].OriginRegionId, defendingRegionId, combatEntry.InvolvedArmies[counter].NumberOfTroops, false);
                    }
                }
            }
        }
        
    // Update Player Information
        function updatePlayers(gameid, eventDone, eventError)
        {
            var data = "?sessionId=" + currentGame.GameId;
            sendAjax("GET", "/api/Game/Players", data, "adv", eventDone, eventError, true);
        }
        
        function playersResponse()
        {
            currentPlayers = JSON.parse(this.responseText);

            var x = 0;
            
            for (x = 0; x < currentPlayers.length; x++)
            {
                if (currentPlayers[x].Name === userToken.userName)
                {
                    addClass("hud", "player-" + currentPlayers[x].Colour);
                }
            }
        }

    // Update Phase Information
        function updateGameState(gameid)
        {
            //console.log("Getting game state information...");

            var data = "?sessionId=" + currentGame.GameId;
            sendAjax("GET", "api/Game/Session", data, "adv", gameStateResponse, gameStateResponse, true)
        }

        function gameStateResponse()
        {
            //console.log("Updating current game state.");

            var gamestate = JSON.parse(this.responseText);

            // PhaseId doesn't change if the type doesn't, but we need to get it at least once
            currentGame.PhaseId = gamestate.PhaseId;
            currentGame.OwnerId = gamestate.OwnerId;

            if (gamestate.PhaseType !== currentGame.PhaseType)
            {
                currentGame.PhaseType = gamestate.PhaseType;
                showPhase(currentGame.PhaseType);

                if (currentGame.PhaseType === 0 || firstRun === true)
                {
                    updatePlayers(currentGame.GameId, playersResponse, playersResponse);
                }

                updateBoard();
            }
        }

    // Game Phase Controller
        function showPhase(phaseType)
        {
            console.log("Entering phase " + phaseType + ".")

            var instruction = "";

            switch (currentGame.PhaseType)
            {
                case 0:
                    instruction = "Waiting for host to begin the game...";
                    break;

                case 1:
                    instruction = "Reinforcements Phase: Deploy reinforcements."
                    updatePlayers(currentGame.GameId, playersResponse, playersResponse);
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
                    instruction = "Redeployment Phase: Not implemented for DevoLAN 31.";

                case 8:
                    instruction = "Victory Phase: Speak up now if you've won!";
                    break;
            }

            writeHTML("hudTextInstructions", instruction);
        }

// Game Phases
    // Territory Interaction
        function territoryInteraction(territoryElement)
        {
            console.log("Resolving interaction with " + territoryElement.id + ".");

            interactionTarget = territoryElement.id;

            switch (currentGame.PhaseType) {
                case 1:
                    console.log("Deploying troop...");
                    deployReinforcements(currentGame.GameId, getData(territoryElement.id, "RegionId"), 1);
                    break;

                case 2:
                    console.log("Resolving attack...");
                    var targetRegionSelection = "<select id='attackTarget'>";
                    var connectedRegions = getData(territoryElement.id, "ConnectedRegions").split(',');
                    
                    for (var index = 0, numberOfRegions = connectedRegions.length; index < numberOfRegions; ++index)
                    {
                        var connectedRegionId = connectedRegions[index];
                        targetRegionSelection += "<option value=\"" + connectedRegionId + "\">" + worldLookup[connectedRegionId] + "</option>";
                    }
                    targetRegionSelection += "</select>";
                    var targetRegionTroops = getData(territoryElement.id, "TroopCount") - 1;
                    if (targetRegionTroops > 0)
                    {
                        var friendlyName = getData(territoryElement.id, "FriendlyName")
                        var data = targetRegionSelection + "<br /><input type=\"number\" id='attackTroops' min=\"1\" value=\"1\" max=\"" + targetRegionTroops + "\" /><br /><input type=\"submit\" id=\"buttonAttackCommit\" value=\"Attack!\"><input type=\"submit\" id=\"buttonAttackCancel\" value=\"Cancel!\">"
                        showOverlay("Attack from " + friendlyName, data);

                        console.log("Starting attack");

                        addEvent("buttonAttackCommit", "click", function () { orderAttack(currentGame.GameId, getData(interactionTarget, "RegionId"), getValue("attackTroops"), getValue("attackTarget")); }, false);
                        addEvent("buttonAttackCancel", "click", function () { hideOverlay(); }, false);
                    }
                    else
                    {
                        showOverlay("Not enough troops", "<img src='Content/images/error.svg' />");
                        setTimeout(hideOverlay, 1500);
                    }
                    break;

                case 7:
                    console.log("Redeploying troop...");
                    
                    break;
            }
        }

    // Reinforcements
        function reinforcementsPhase(gameid)
        {
            console.log("Getting reinforcements.");

            var data = "?sessionId=" + currentGame.GameId;
            sendAjax("GET", "api/Nation/Reinforcements", data, "adv", reinforcementsResponse, null, true);
        }

        function reinforcementsResponse()
        {
            currentPhase.reinforcements = +this.responseText;
            writeHTML("hudTextInformation", "You have " + this.responseText + " reinforcements to deploy.");
        }

        function deployReinforcements(GameId, RegionId, troops)
        {
            console.log("Deploying troops to " + RegionId + ".");
            
            if (currentPhase.reinforcements > 0)
            {
                currentPhase.reinforcements -= 1;

                var data = "?sessionId=" + currentGame.GameId + "&regionId=" + RegionId + "&numberOfTroops=" + troops;
                sendAjax("POST", "api/Region/Deploy", data, "adv", deployResponse, deployResponse, true);
            }
            else
            {
                showOverlay("No more troops to deploy!", "<img src='Content/images/error.svg' />");
                setTimeout(hideOverlay, 1500);
            }
        }

        function deployResponse()
        {
            console.log("Resolving troop deployment order.");

            switch (this.status)
            {
                case 200:
                    var TroopCount = +getData(interactionTarget, "TroopCount") + 1;
                    writeHTML("hudTextInformation", "You have " + currentPhase.reinforcements + " reinforcements to deploy.");
                    writeHTML(interactionTarget + "-counter", TroopCount);
                    setData(interactionTarget, "TroopCount", TroopCount)
                    break;
                case 406:
                    currentPhase.reinforcements += 1;
                    showOverlay("You don't own this region", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
                default:
                    currentPhase.reinforcements += 1;
                    showOverlay("No more troops to deploy!", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
            }
        }

    // Combat
        function orderAttack(gameId, sourceRegionId, numberOfTroops, targetRegionId)
        {
            console.log("Attacking from " + sourceRegionId + " to " + targetRegionId);
            createOrUpdateAttack(sourceRegionId, targetRegionId, numberOfTroops, true);

            var data = "?sessionId=" + gameId + "&regionId=" + sourceRegionId + "&numberOfTroops=" + numberOfTroops + "&targetRegionId=" + targetRegionId;
            sendAjax("POST", "api/Region/Attack", data, "json", onOrderAttackResponse, onOrderAttackResponse, true);
        }

        function onOrderAttackResponse()
        {
            switch(this.status){
                case 200:
                case 204:
                    console.log("Attack order successful");
                    hideOverlay();
                    break;
                case 400:
                    console.log("Attack failed. Troops already committed");
                    showOverlay("Order failed: Not enough troops.", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
                case 402:
                    console.log("Attack failed. Regions not connected");
                    showOverlay("Order failed: Regions not connected.", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
                case 404:
                    console.log("Attack failed. Invalid region id");
                    showOverlay("Order failed: Invalid region.", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
                case 406:
                    console.log("Attack failed. Not owner of source, or owner of target");
                    showOverlay("Order failed: You don't own that territory!", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
                case 417:
                    console.log("Attack failed. Invalid session phase");
                    showOverlay("Order failed: The orders phase is over.", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
                default:
                    console.log("Attack failed. Unknown error..");
                    showOverlay("Order failed: Unknown issue, sorry...", "<img src='Content/images/error.svg' />");
                    setTimeout(hideOverlay, 1500);
                    break;
            }
        }
        
// Hud
        function showHud()
        {

        }

        function hideHud()
        {

        }

        function endTurn(gameId, phaseId)
        {
            // Technically, only the session host can do this. Anyone else will get an error and be ignored - they should EndPhase, but for DevoLAN 31 that doesn't actual do anything!
            if (currentGame.OwnerId === userToken.id)
            {
                var data = "?sessionId=" + gameId + "&phaseId=" + phaseId + "&force=false";
                sendAjax("POST", "/api/Game/AdvanceNextPhase", data, "json", onEndTurnResponse, onEndTurnResponse, true);
            }
            else
            {
                var data = "?sessionId=" + gameId + "&phaseId=" + phaseId;
                sendAjax("POST", "/api/Game/EndPhase", data, "json", onEndTurnResponse, onEndTurnResponse, true);
            }
        }

        function onEndTurnResponse()
        {
            switch (this.status)
            {
                case 200:
                case 204:
                    console.log("Advanced to next phase");
                    updateGameState(currentGame.GameId);
                    break;
                case 401:
                    console.log("Not owner of session");
                    messageBox("End Turn Failed.", "Only the session owner is allowed to end the turn");
                    break;
                case 417:
                    console.log("Not all players are ready");
                    messageBox("End Turn Failed.", "Waiting for other players to be ready...");
                    break;
                default:
                    console.log("End turn request failed.");
                    messageBox("End Turn Failed.", "Not sure why -  Error code " + this.status);
                    break;
            }
        }

// Message Box
        function messageBox(title, message)
        {
            hideOverlay();

            writeHTML("message", "<h3>" + title + "</h3><p>" + message + "</p>");
            replaceClass("message", "show");

            setTimeout(function ()
            {
                replaceClass("message", "hide");
            }, 5000);
        }
        
// Overlay
        function showOverlay(title, content, blackout)
        {
            //console.log("Showing overlay.");
        
            writeHTML("overlayContent", "<h1>" + title + "</h1>" + content);
            addClass("overlay", "show");
        
            if (blackout)
            {
                addClass("overlay", "blackout", 1);
            }
        }
    
        function hideOverlay(fade)
        {
            //console.log("Hiding overlay.");
        
            if (fade)
            {
                removeClass("overlay", "blackout");
                setTimeout(hideOverlay, 1500);
            }
            else
            {
                removeClass("overlay", "show");
            }
        }
    
// Save User Login Session
        function saveCookie(name, data)
        {
            document.cookie = name + "=" + JSON.stringify(data);
        }
    
        function loadCookie(name)
        {
            var cookies = document.cookie.split(";");
            var x = 0;

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
    
        function deleteCookie(name)
        {
            document.cookie = name + "=''; expires=Thu, 01 Jan 1970 00:00:00 UTC";
        }
    
// Menu & Settings
    // Menu
        function menuRefresh()
        {
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
        function toggleMusic()
        {
            if (system.music == 0)
            {
                system.music = 1;
                playAudio("music");
            }
            else
            {
                system.music = 0;
                stopAudio("music");
            }
            
            menuRefresh();
        }
        
    // Toggle SFX
        function toggleSFX()
        {
            if (system.sfx == 0)
            {
                system.sfx = 1;
                playAudio("sfx");
            }
            else
            {
                system.sfx = 0;
                stopAudio("sfx");
            }
            
            menuRefresh();
        }
        
    // Audio Handling
        function playAudio(target)
        {
            console.log("Playing " + target + ".");
        
            var player = getID(target);
        
            switch (target)
            {
                case "sfx":
                    if (system.sfx)
                    {
                        player.play();
                    }
                    break;
                
                case "music":
                    if (system.music)
                    {
                        player.play();
                    }
                    break;
            }
        }

        function loadAudio(target, sound)
        {
            console.log("Loading " + sound + " on " + target + " channel.");
        
            var source = getID(target + "Source");
        
            stopAudio(target);
            source.src = "Content/media/" + sound + ".mp3";
            getID(target).load();
        
            playAudio(target);
        }
        
        function stopAudio(target)
        {
            console.log("Stopping " + target + ".");
        
            getID(target).pause();
        }
    
        function setAudioVolume(target, level)
        {
            //console.log("Adjusting " + target + " to volume level " + level + ".");
        
            var player = getID(target);
            player.volume = level;
        }
    
        function fadeAudio(target, level, duration)
        {
            //console.log("Fading " + target + " to " + level + " over " + duration + " seconds.");
        
            var player = getID(target);
            var initial = player.volume;

            duration = (duration * 1000) / (duration * 25);
            level = initial / duration;
        
            var timer = setInterval(function ()
            {
                try
                {
                    player.volume -= level;
                }
                catch (err)
                {
                    player.volume = 0;
                    clearInterval(timer);
                }

                if (player.volume === 0.0)
                {
                    stopAudio(target);
                    setAudioVolume(target, initial)
                    clearInterval(timer);
                }
            }, duration);
        }

// Map helpers
        function getRegionMapElementIdByGuid(sourceRegionId)
        {
            var regionName = worldLookup[sourceRegionId];
            return getRegionMapElementIdByName(regionName);
        }

        function getRegionMapElementIdByName(sourceRegionName)
        {
            return "territory" + sourceRegionName.replace(/ /g, "");
        }

        function createOrUpdateAttack(sourceRegionId, targetRegionId, troopCount, appendToExisting)
        {
            var sourceRegionMapElement = getID(getRegionMapElementIdByGuid(sourceRegionId));
            var existingAttacks = sourceRegionMapElement.getElementsByTagName("g");
            for(var counter = 0; counter < existingAttacks.length; ++counter)
            {
                if(getDataOnElement(existingAttacks[counter], "target-region") === targetRegionId)
                {
                    // Update this element
                    var textElement = existingAttacks[counter].getElementsByTagName("text")[0];
                    if (appendToExisting)
                    {
                        textElement.textContent = parseInt(textElement.textContent) + parseInt(troopCount);
                    }
                    else
                    {
                        textElement.textContent = troopCount;
                    }
                    return;
                }
            }
            
            // Failed to find existing element, draw a new one
            drawAttack(sourceRegionMapElement, targetRegionId, troopCount);
        }

        function removeAllAttacks(sourceRegionMapElement)
        {
            // Eventually, we'll just hide the element as it'll save the effort of re-adding it later
            // For now, let's just delete it!
            var existingAttackGroups = sourceRegionMapElement.getElementsByTagName("g");
            for(var counter = 0; counter < existingAttackGroups.length; ++counter)
            {
                sourceRegionMapElement.removeChild(existingAttackGroups[counter]);
            }
        }

        function drawAttack(sourceRegionMapElement, targetRegionId, troopCount)
        {
            var targetRegionMapElement = getID(getRegionMapElementIdByGuid(targetRegionId));

            // Draw line between two, using the colour of the source region
            var sourceRegionCircle = sourceRegionMapElement.getElementsByTagName("circle")[0];
            var targetRegionCircle = targetRegionMapElement.getElementsByTagName("circle")[0];
            
            var source = new Victor(parseFloat(sourceRegionCircle.getAttribute("cx")), parseFloat(sourceRegionCircle.getAttribute("cy")));
            var target = new Victor(parseFloat(targetRegionCircle.getAttribute("cx")), parseFloat(targetRegionCircle.getAttribute("cy")));

            var sourceToTarget = target.clone().subtract(source).normalize();
            var radius = parseFloat(sourceRegionCircle.getAttribute("r"));
            var radiusOffsetVector = sourceToTarget.clone().multiply(new Victor(radius, radius));
            var sourceToTargetNormal = new Victor(-sourceToTarget.y * radius, sourceToTarget.x * radius);

            var sourceOffset = source.clone().add(radiusOffsetVector);
            var sourceLhsOffset = source.clone().add(sourceToTargetNormal);
            var sourceRhsOffset = source.clone().subtract(sourceToTargetNormal);
            var targetOffset = target.clone().subtract(radiusOffsetVector);
            var textOffset = sourceOffset.clone().add(radiusOffsetVector).add(radiusOffsetVector);

            // Create group for attack arrow
            var attackGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
            setDataOnElement(attackGroup, "target-region", targetRegionId);
            sourceRegionMapElement.insertBefore(attackGroup, sourceRegionCircle);

            // Draw attack arrow
            var attackArrow = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
            attackArrow.setAttribute('class', 'attack-' + sourceRegionCircle.getAttribute('class'));
            attackArrow.setAttribute('points',
                vectorToCommaString(sourceOffset) + ' ' +
                vectorToCommaString(sourceLhsOffset) + ' ' +
                vectorToCommaString(targetOffset) + ' ' +
                vectorToCommaString(sourceRhsOffset));
            attackGroup.appendChild(attackArrow);

            // Draw troop count on attack
            var attackText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            attackText.setAttribute('class', 'counter');
            attackText.setAttribute('transform', 'translate(' + vectorToCommaString(textOffset) + ')');
            attackText.textContent = troopCount;
            attackGroup.appendChild(attackText);
        }

        function vectorToCommaString(targetVector) 
        {
            return targetVector.x + ',' + targetVector.y;
        }

// Screen Sizing
        function resizeBoard()
        {
            var x = 0;
            var circles = document.getElementsByTagName("circle");
            var r = 17.5;
                
            for (x = 0; x < circles.length; x++)
            {
                if (window.innerWidth < 1100 || window.innerHeight < 650)
                {
                    r = 35;
                }
                else
                {
                    r = 17.5;
                }
                    
                circles[x].setAttribute("r", r);
            }
        }
        
// Fill
    // Set Fill Colour
        function setFillColour(target, colour)
        {
            getID(target).style.fill = colour;
        }
    
// Stroke
    // Set Stroke Colour
        function setStrokeColour(target, colour)
        {
            getID(target).style.stroke = colour;
        }
        
    // Set Stroke Width
        function setStrokeWeight(target, weight)
        {
            getID(target).style['stroke-width'] = weight;
        }
        
// Text
    // Set Text Value
        function setTextValue(target, value)
        {
            getID(target).textContent = value;
        }
        
// AJAX Requests
        function sendAjax(method, url, urlData, content, eventLoad, eventError, auth, sendData, noURI)
        {
            var ajaxRequest = new XMLHttpRequest();
        
            if (noURI)
            {
                ajaxRequest.open(method, url + urlData);
            }
            else
            {
                ajaxRequest.open(method, apiUriBase + url + urlData);
            }
        
            if (eventLoad)
            {
                ajaxRequest.addEventListener("load", eventLoad);
            }

            if (eventError)
            {
                ajaxRequest.addEventListener("error", eventError);
            }
            
            if (auth)
            {
                ajaxRequest.setRequestHeader("Accept", "text/html");
                ajaxRequest.setRequestHeader("Authorization", "bearer " + userToken.access_token);
            }
        
            switch (content)
            {
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
        function addClass(target, clss)
        {
            getID(target).classList.add(clss)
        }

        function replaceClass(target, clss)
        {
            var targetElement = getID(target);
            for (var i = targetElement.classList.length - 1; i >= 0; i--)
            {
                targetElement.classList.remove(targetElement.classList.item(i));
            }
            targetElement.classList.add(clss)
        }

        function removeClass(target, clss)
        {
            if (checkClass(target, clss))
            {
                getID(target).classList.remove(clss);
            }
        }
        
        function toggleClass(target, clss)
        {
            getID(target).classList.toggle(clss);
        }

        function checkClass(target, clss)
        {
            return getID(target).classList.contains(clss);
        }
        
    // Data
        function getData(target, data)
        {
            return getDataOnElement(getID(target), data);
        }

        function getDataOnElement(target, data)
        {
            return target.getAttribute("data-" + data);
        }
        
        function setData(target, data, value)
        {
            setDataOnElement(getID(target), data, value);
        }

        function setDataOnElement(target, data, value)
        {
            target.setAttribute("data-" + data, value);
        }
        
    // Get Element by ID
        function getID(target)
        {
            return document.getElementById(target);
        }

    // Styles
        function writeStyle(t, v)
        {
            document.getElementById(t).setAttribute("style", v);
                
            return;
        }
        
    // Event Listeners
        function addEvent(target, evnt, func, c)
        {
            getID(target).addEventListener(evnt, func, c);
        }
        
    // innerHTML
        function writeHTML(target, value, append)
        {
            if (append === true)
            {
                document.getElementById(target).innerHTML += value;
            }
            else
            {
                document.getElementById(target).innerHTML = value;
            }
        }

    // Text Content
        function setTextContent(target, text)
        {
            getID(target).textContent = text;
        }
    
    // Values
        function getValue(target)
        {
            return getID(target).value;
        }
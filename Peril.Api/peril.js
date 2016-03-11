// Board
	// Setup Board
		function boardSetup(){
			// Resize Listener
				window.addEventListener("resize", function(){resizeBoard()}, true);
				resizeBoard();
		}

// Screen Sizing
	// Circle Sizer
		function resizeBoard(){
				var x = 0;
				var circles = document.getElementsByTagName("circle");
				var r = 17.5;
				
				for(x = 0; x < circles.length; x++){
					if(window.innerWidth < 1100 || window.innerHeight < 600){
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
	
// Get Element by ID
	function getID(target){
		return document.getElementById(target);
	}
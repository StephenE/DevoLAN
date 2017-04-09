
// Draw an attack arrow between two points
function drawAttack(sourceRegionMapElement, targetRegionId, troopOwnerId, troopCount, drawAsBorderClash)
{
    var targetRegionMapElement = getID(getRegionMapElementIdFromGuid(targetRegionId));
    var playerColour = getPlayerColour(troopOwnerId);
    var uniqueId = drawAsBorderClash ? getRegionBorderClashMapElementNameFromGuid(targetRegionId) : getRegionAttackMapElementNameFromGuid(targetRegionId);

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
    if (drawAsBorderClash === true)
    {
        var targetOffset = target.clone().add(sourceToTargetNormal);
    }
    else
    {
        var targetOffset = target.clone().subtract(radiusOffsetVector);
    }
    var textOffset = sourceOffset.clone().add(radiusOffsetVector).add(radiusOffsetVector);

    // Create group for attack arrow
    var attackGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    setDataOnElement(attackGroup, "target-region", uniqueId);
    attackGroup.setAttribute('class', 'attack-hidden');
    sourceRegionMapElement.insertBefore(attackGroup, sourceRegionCircle);

    // Draw attack arrow
    var attackArrow = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
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

    return attackGroup;
}

// Change visibility on all attacks attached to a region
function setAttackElementVisbility(sourceRegionMapElement, showBorderClashes)
{
    var existingAttacks = sourceRegionMapElement.getElementsByTagName("g");
    var prefixToShow = showBorderClashes ? "BorderClash" : "Attack";
    for (var counter = 0; counter < existingAttacks.length; ++counter)
    {
        var targetRegionId = getDataOnElement(existingAttacks[counter], "target-region");
        if (targetRegionId.startsWith(prefixToShow))
        {
            replaceClassOnElement(existingAttacks[counter], "attack-player-1");
            var textElement = existingAttacks[counter].getElementsByTagName("text")[0];
            textElement.textContent = "99";
        }
        else
        {
            replaceClassOnElement(existingAttacks[counter], "attack-hidden");
        }
    }
}
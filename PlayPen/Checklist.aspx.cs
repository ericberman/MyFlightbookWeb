using MyFlightbook;
using MyFlightbook.Checklists;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class PlayPen_Checklist : Page
{
    protected string[] rgSamples =
    {
        @"TAB Inspections
- CABIN
[] Maintenance...SQUAWKS, VOR CHECK CURRENT
[] Manuals & Papers...ON BOARD
[] Avionics Master...OFF
[] Switches...OFF (except ELECTRIC TRIM switch)
[] Master Switch...ON
[] Flaps...EXTENDED
[] Aircraft Log...HOBBS TIME RECORDED (PRESS LF+DIM ON JPI ENGINE UNIT)
[] Cowl Flaps...OPEN
[] Fuel Quantity...GAUGES CHECKED
[] Landing Lights/Strobes...ON, (CHECKED FOR NIGHT FLIGHT) - OFF
[] Pitot Heat...ON (CHECK AS REQUIRED FOR IMC) - OFF
[] Fuel Sump (Center Floor)...DRAIN – (PULL AND HOLD FOR ~ 3 SECS)
[] Master Switch...OFF

- RIGHT WING
[] Ailerons & Flaps...PINS, RODS, HINGES & STATIC WICKS
[] Nav Lights, Strobe...CHECKED (Night Flight)
[] Leading Edge...CLEAN & CLEAR (DENTS & DAMAGE)
[] Gear Doors...CHECKED – TIE RODS, COTTER PINS, WELDS, ETC.
[] Tires & Struts...CHECKED, SCREWS TIGHT
[] Speed Brakes...STOWED
[] Landing Lights...CHECKED (Night Flight)
[] Tie Down & Chock...REMOVED
[] Fuel Level...CHECKED – CAP SECURE
[] Fuel Sample...CLEAN & CLEAR (SEDIMENT & WATER)

- NOSE
[] Oil...6 Quarts; CAP TIGHT – DO NOT OVERTIGHTEN
[] Cowl Flaps (L&R)...OPEN – CHECK TIE RODS, COTTER PINS, ETC.
[] Tow Bar...REMOVED and STOWED
[] Alternator Belt...PROPER BELT TENSION
[] Prop...CHECKED
[] Spinner...SCREWS TIGHT
[] Windshield...CLEAN, NO CRACKS, CHIPS or SCRATCHES
[] Static Drain...DRAINED & CHECKED

- LEFT WING

[] Fuel Sample...CLEAN & CLEAR (SEDIMENT & WATER)
[] Tires & Struts...CHECKED, SCREWS TIGHT
[] Gear Doors...CHECKED – RODS & WELDS
[] Leading Edge...CHECKED
[] Stall Warning Tab...CLEAR, FREE
[] Landing, Taxi Lights...CHECKED (Night Flights)
[] Nav Lights, Strobe...CHECKED
[] Pitot Tube...CLEAR, WARM (IMC)
[] Ailerons & Flaps...PINS, RODS, HINGES & STATIC WICKS
[] Tie Down & Chock...REMOVED
[] Speed Brakes...STOWED
[] Static System Drain...DRAINED & CHECKED

- EMPENNAGE
[] Antennas...CHECKED
[] Elevator...HINGES, CONTROL ROD & PINS
[] Tail, Vertical Stab...HINGES, CONTROL ROD & PINS
[] Baggage Compartment...DOOR CLOSED & LOCKED (Load secure & within W&B limitations)
[] Tail Tie-down...REMOVED & STOWED

Tab Pre-Flight
- Before Engine Start
[] Seats/Seat Belts, Door...ON, LOCKED & LATCHED
[] Gear Lever...DOWN
[] Emergency Gear Handle...DOWN AND LATCHED
[] All Switches...OFF (except ELECTRIC TRIM switch)
[] ELT Switch...ARMED (center position)
[] Circuit Breakers...CHECKED (in)
[] Cabin Vent, Heat, Defrost...CLOSED
- Starting Engine
[] Brake(s)...SET
[] Master switch (ONLY)...ON (ALT SWITCH OFF)
[] Annunciator Panel...PRESS TO TEST (LEFT/RIGHT FUEL AND ALTERNATOR WILL STAY OFF)
[] Volt Meter...CHECK FOR POSITIVE VOLTAGE
[] Cowl Flaps...OPEN
[] Wing Flaps...FLAPS UP 
[] Fuel Selector...FULLEST TANK (or left tank)
[] Beacon Light (Strobe)...ON
[] Mixture...RICH (FULL FORWARD)
[] Prop...IN (FULL FORWARD)
[] Throttle – MP...OPEN ¼” (FORWARD)
[] Fuel Pump...ON (4 secs) – POSITIVE PRESSURE – OFF
[] Mixture...IDLE CUTOFF (FULL BACK)
[] Prop Area...CLEAR
[E+] Starter...ENGAGE
[] Mixture...SMOOTHLY FWD~1¼” BACK AT TAXI
[] Throttle – MP...1000-1200 RPM
[] Alternator Switch...ON (CHECK POSITIVE AMPS)
[] Oil, Fuel Pressure...GREEN WITHIN 10 SEC
[] Start Power Light...OFF
[] Avionics Master...ON
[] Auto Pilot...TEST (warn pax about loud beep)
[] PFD...PRESS “AP TEST” 
[] Fuel levels...CHECKED, RESET AS REQUIRED
[] Transponder...STANDBY 
[] Flight Instruments...CHECKED (FLAGS) AND SET 
[] Headphones...CHECK IN “STEREO” MODE
[] Clearance ...RECV’D, RECORDED, READ BACK
[] ATIS...RECORDED
[] Taxi (IFR w/ ATIS)...RECV’D, RECORDED, READ BACK

- BEFORE TAKEOFF – RUNUP
[] Doors & Windows...CLOSED AND LATCHED
[] Flight Controls...FREE AND CORRECT 
[] Autopilot...OFF
[] Elevator Trim...TAKEOFF POSITION
[] Flight Instruments...CHECKED AND SET (FLAGS)
[] Parking Brake...SET
[] Oil Temperature...WARM (at least 85° F)
[] Cowl Flaps...OPEN
[] Fuel Selector...FULLEST TANK (OR LEFT)
[] Mixture...RICH
[] Propeller...FULL FORWARD
[] Throttle...2000 RPM
[] ● Magnetos...CHECK (Max drop 175 RPM)
[] ● Prop...CYCLE 3 TIMES, (RPM: ↓ 300-400 max: MANIFOLD PRES: ↑;- OIL PRESSURE: ↓)
[] ● Engine Instruments...GREEN, (POSITIVE VOLTAGE, AMPS)
[] ● Annunciator Panel...TEST
[] Throttle...1000 RPM
[] Radios...SET (COMM, NAV, GPS)
[] Transponder...SET  (ALTITUDE & SQUAWK CODE)
[] Speed Brakes...RETRACTED
[] VOR Check...CURRENT (for IFR flight)

TAB In Flight
- NORMAL TAKEOFF
[] Flaps...TAKEOFF POSITION
[] Mixture...RICH (forward)
[] Prop...FULL FORWARD
[] Fuel Boost Pump...ON  (CHECK PRESSURE)
[] Lights...AS REQUIRED
[] Speed Brakes...CHECKED AND RETRACTED
[] Tower Clearance...Takeoff  (Heading, Altitude, Frequency)
[] Time...START
[] Throttle...ADVANCE SMOOTHLY
[] Engine Instruments...CHECK (GREEN, Airspeed alive)
[] Rotate...60-65 KIAS
[] Initial Climb...86 KIAS (VY) (Climb checklist)
[] Landing Gear...RETRACT (W/ POSITIVE RATE of CLIMB)
[] Flaps...RETRACT (CLEAR OF OBSTACLES)
 
- CLIMB
[] Best Rate (Vy)...86 KIAS, PITCH FOR 
[] Best Angle (Vx)...66 KIAS, PITCH FOR 
[] Enroute ...100 KIAS, PITCH FOR 
[] Fuel Pump...OFF (UPON REACHING SAFE ALTITUDE)
[] Prop...2600 RPM
[] Mixture...LEAN (ABOVE 3000' MSL)

- LEVEL OFF / CRUISE
[] Cowl Flaps...CLOSE - (CHT < 350° F). 
[] Pitch Level...REACH CRUISING SPEED 
[] Power...SET (Reference Power Setting Table)
[] Trim...AS REQUIRED
[] Mixture...LEAN (EGT 75-100° F RICH OF PEAK)

- DESCENT
[] Cowl Flaps...CHECK CLOSED
[] Throttle...ADJUST FOR DESCENT (~20” MP)
[] Airspeed...MONITOR 
[] Mixture...ADJUST (enrich from high-altitude lean setting)

- BEFORE LANDING 
[] ATIS...RECORDED
[] Radios...SET (Approach Control, Tower, ILS, Missed Approach)
[] Cowl Flaps...CLOSED
[] Gas: - Tank...FULLEST TANK
[] Mixture...RICH
[] Fuel Pump...ON
[] UNDERCARRIAGE...DOWN & LOCKED (EMERGENCY HANDLED LOCKED)
[] M P (Throttle)...DOWNWIND SETTING 
[] Prop...FULL FORWARD
[] Safety & Switches...SEAT BELTS FASTENED – LIGHTS (AS REQ’D)
 
- NORMAL LANDING
[] Cowl Flaps...CLOSED - 
[] Gas...FULL TANK; PUMP ON; MIXTURE RICH
[] Undercarriage GEAR...VERIFIED DOWN & LOCKED
[] Manifold Pressure...SET 
[] Prop...SET (Full)
[] Safety & Switches...SEAT BELT, DOORS, & LIGHTS

Tab Post Flight
- AFTER LANDING
[] Cowl Flaps...OPEN
[] Wing Flaps...RETRACT
[] Transponder...STANDBY
[] Lights...TAXI, NAV – (STROBES & RECOGNITION OFF)
[] Fuel Pump...OFF
[] Taxi Instructions...RECEIVED & READ BACK
[] Mixture...LEAN FOR TAXI

- STOPPING ENGINE
[] Avionics Master...OFF (down)
[] Lights...OFF
[] Alternator Switch...OFF
[] Mixture ...IDLE CUT OFF (OUT)
[] Throttle...FULL IDLE (OUT)
[E-] Pause...ALLOW THE ENGINE TO FULLY STOP
[] Ignition...OFF
[] Ending Hobbs...RECORD
[] Master Switch...OFF
[] Backup EFIS...OFF

- SECURING AIRPLANE
[] Position...IN HANGAR/TIE DOWN SPACE
[] Yokes...IF OUTSIDE, SECURE WITH CONTROL LOCK OR SEAT BELTS
[] Squawks...RECORDED IN LOG
[] Doors...LATCH AND LOCK IF OUTSIDE; OPEN IN HANGAR
[] Aircraft Cover...ON AND SECURE IF OUTSIDE
[] Tie Downs...SECURE
[] Wheel Chocks...IN PLACE
[] Fuel...TO TABS 

TAB Procedures
- ABNORMAL ENGINE START PROCEDURES--
-- FLOODED ENGINE START PROCEDURE
[] Fuel Boost Pump...OFF
[] Mixture...FULL LEAN (BACK)
[] Throttle...FULL OPEN
[] Starter...ENGAGE (FULL CLOCKWISE, THEN PUSH)
[] Mixture...SMOOTHLY FORWARD WHEN ENGINE FIRES, THEN ~1¼” BACK
[] Throttle...SET TO 1000 RPM

--WARM ENGINE START PROCEDURE
[] Fuel Boost Pump...OFF
[] Mixture...FULL LEAN (BACK)
[] Throttle...SLIGHTLY OPEN (~1” FORWARD)
[] Starter...ENGAGE (FULL CLOCKWISE, THEN PUSH)
[] Mixture...SMOOTHLY FORWARD WHEN ENGINE FIRES, THEN ~1¼” BACK
[] Throttle...SET TO 1000 RPM

-- EXTERNAL POWER SOURCE START PROCEDURE
[] Master Switch...ON (ALT OFF)
[] All Electrical Equipment...OFF
[] Terminals...CONNECT
[] External Power Plug...INSERT IN FUSELAGE
[] Proceed With Normal Start...THROTTLE AT LOW RPM
[] External Power Plug...DISCONNECT FROM FUSELAGE
[] ALT Switch...ON (check ammeter)
[] Oil Pressure...CHECK
 
- ABNORMAL TAKEOFFS
-- SHORT FIELD/OBSTACLE CLEARANCE TAKEOFF
[] Speed Brakes...CHECKED & RETRACTED
[] Flaps...TAKEOFF POSITION
[] Position...RUNWAY CENTERLINE 
(using all available runway)
[] Brakes...HOLD
[] Throttle...ADVANCE (smoothly to full power)
[] Engine Instruments...CHECK (“Gauges green, RPMs alive”)
[] Brakes...RELEASE
[] Rotate...60-65 KIAS (depending on aircraft weight)
[] Initial Climb...66 KIAS (VX)
[] Flaps...RETRACT AFTER OBSTACLE CLEARANCE
[] Climb After Obstacle Clearance...86 KIAS
[] Landing Gear...RETRACT WHEN CLEAR OF RUNWAY, BELOW 107 KNOTS

-- SOFT FIELD TAKEOFF
[] Speed Brakes...CHECKED AND RETRACTED
[] Flaps...TAKEOFF POSITION
[] Elevator...FULL BACK PRESSURE
[] Throttle...ADVANCE (smoothly to full power)
[] Rotate...ASAP (lift off at lowest possible airspeed)
[] Accelerate...IN GROUND EFFECT
[] Initial Climb...66 KIAS (VX)
[] Flaps...RETRACT AFTER OBSTACLE CLEARANCE
[] Climb After Obstacle Clearance...86 KIAS
[] Landing Gear...RETRACT WHEN CLEAR OF RUNWAY, BELOW 107 KNOTS
 
- NON-STANDARD LANDINGS
-- SHORT FIELD LANDING
[] Flaps...TAKEOFF/DOWN (on final)
[] Landing Gear...EXTENDED
[] Trim...FOR TARGET POINT
[] Throttle...AS REQUIRED
[] Level Out...IN GROUND EFFECT
[] Throttle...RETARD to FULL IDLE
[] Rotate...MAIN GEAR ONTO GROUND
[] Flaps...RETRACT (as soon as safely possible)
[] Brakes...APPLY TO MAXIMUM
[] Elevator...FULL BACK PRESSURE
[] Exit Runway...AS SOON AS SAFELY POSSIBLE

-- SOFT FIELD LANDING
[] Flaps...TAKEOFF/DOWN (on final)
[] Landing Gear...EXTENDED
[] Trim...FOR TARGET POINT
[] Throttle...AS REQUIRED 
[] Level Out...IN GROUND EFFECT
[] Throttle...RETARD AS APPROPRIATE (may require additional of small amount of power just prior to touchdown)
[] Rotate...MAIN GEAR ONTO GROUND (keep nose high)
[] Nose Wheel...LOWER SLOWLY (maintaining back pressure as long as possible)
[] Exit Runway...AS SOON AS SAFELY POSSIBLE
 
TAB EMERGENCY
- ENGINE FIRE DURING START
[] Starter...CRANK ENGINE, KEEP ENGAGED
--If engine starts…
[] Throttle...1500 RPM for several minutes or until fire is extinguished
[] Engine...SHUTDOWN (inspect for damage)
--If engine does NOT start…
[] Starter...CONTINUE CRANKING
[] Mixture...IDLE CUTOFF
[] Throttle...FULL FORWARD
[] Fuel Selector ...OFF
[] Starter...OFF
[] Master Switch...OFF
[] Extinguish Fire...FIRE EXTINGUISHER
[] Aircraft...ABANDON IF FIRE CONTINUES

- ENGINE POWER LOSS DURING TAKEOFF
--If sufficient runway remains for normal landing, land straight ahead
--If insufficient runway remains:
Maintain safe airspeed
Make only shallow turns to avoid obstructions
Flaps as situation requires
--If sufficient altitude has been gained to attempt a restart:
[] Maintain Safe Airspeed...84-93 KIAS
[] Fuel Selector...SWITCH TO FULLEST TANK 
[] Master Switch...ON
[] Fuel Boost Pump...CHECK ON
[] Throttle...FULL FORWARD
[] Propeller...FULL FORWARD
[] Mixture...FULL FORWARD
[] Ignition Switch...ON “BOTH”
[] If Power Is Not Regained...PROCEED WITH POWER OFF LANDING –[NEXT PAGE]
 
- ENGINE ROUGHNESS
[] Engine Gauges...CHECK
[] Fuel Boost Pump...ON
[] Fuel Selector...SWITCH TANKS
[] Mixture...ADJUST
[] Ignition Switch...LEFT, THEN RIGHT, THEN BOTH 
[] Checklist...REVIEW POWER-OFF LANDING CHECKLIST

- ENGINE LOSS IN FLIGHT
[] Pitch...FOR BEST GLIDE L/DMAX
[] Turn...PICK A PLACE TO LAND
[] Trim...FOR BEST GLIDE L/DMAX
[] Altitude Permitting:...RESTART IN FLIGHT
[] Tanks...SWITCH
[] Master Switch...ON
[] Fuel Boost Pump...ON
[] Throttle...FULL FORWARD
[] Propeller...FULL FORWARD
[] Mixture...FULL FORWARD
[] Ignition Switch...CHECK ON “BOTH”
[] Engine Gauges...CHECK (for indication of problem)
[] Checklist...POWER-OFF LANDING CHECKLIST

- POWER OFF LANDING
[] Locate...SUITABLE LANDING SPOT
[] Establish...WIND DIRECTION
[] Emergency Locator Transmitter...ARMED
[] Door...UNLATCHED AND WEDGED OPEN
[] Seats and Seat Belts...TIGHT AND LOCKED
[] Fuel Selector...OFF
[] Mixture...IDLE CUT OFF (FULL BACK)
[] Ignition Switch...OFF
[] Flaps...FULL DOWN
[] Landing Gear...DOWN or UP (depending on terrain) 
[] Master Switch, Alternator Switch...OFF

- ENGINE FIRE IN FLIGHT
[] Fuel Selector...OFF
[] Throttle...CLOSED (FULL BACK)
[] Mixture...IDLE CUT OFF (FULL BACK))
[] Fuel Boost Pump...OFF
[] Cabin Ventilation & Heating...CLOSED/OFF
[] Ignition Switch...OFF
[] Cowl Flaps...CLOSED
[] Landing Gear...DOWN or UP (depending on terrain)
[] Flaps...EXTEND AS NECESSARY
[] If fire not extinguished - ...INCREASE GLIDE SPEED 
OPEN COWL FLAPS
DO NOT ATTEMPT RESTART
Conduct Power-off Landing Checklist
 
- OIL PRESSURE LOW – BELOW 25 PSI
[] Land...AS SOON AS PRACTICAL AND INVESTIGATE CAUSE
Review Power-off Landing Checklist

- FUEL PRESSURE LOW
[] Mixture...ENRICH
[] Fuel Selector...CHECK ON FULLEST TANK; SWITCH TANKS IF NECESSARY
[] If Condition Persists, Land...AS SOON AS PRACTICAL AND INVESTIGATE CAUSE
[] Fuel Boost Pump...ON
Review Power-off Landing Checklist

- HIGH OIL TEMPERATURE
[] Cowl Flaps...OPEN
[] Airspeed...INCREASE
[] Power...REDUCE
[] If Condition Persists, Land...AS SOON AS PRACTICAL AND INVESTIGATE CAUSE
Review Power-off Landing Checklist

- PROPELLER OVERSPEED
[] Throttle...RETARD
[] Oil Pressure...CHECK
[] Propeller...DECREASE IF CONTROL POSSIBLE
[] Airspeed...REDUCE
[] Throttle...AS REQUIRED TO KEEP RPM BELOW 2700
 
- LANDING GEAR FAILS TO EXTEND
[] Landing Gear Circuit Breaker...PULL
[] Landing Gear Switch...DOWN
[] Manual Gear Extension...LATCH FORWARD, LEVER BACK
[] T-Handle...PULL SLOWLY ~14 - 18 TIMES, STOP WHEN RESISTANCE FELT
[] Visual Gear Down Indicator...CHECK GREEN

- ELECTRICAL / ALTERNATOR FAILURE/LOW
[] Diagnostic:...VOLTAGE WARNING LIGHT FLASHING, AMMETER SHOWING DISCHARGE
*If Failed:
[] Alternator Switch...OFF
[] Reduce Electrical Load ...TURN OFF ALL NON-ESSENTIAL ELECTRICAL EQUIPMENT
[] Alternator Circuit Breaker...CHECK, RESET ONCE IF NECESSARY
[] Alternator Switch...ON
**If Power Not Restored...
[] Alternator Switch...OFF
[] Land...AS SOON AS PRACTICAL AND INVESTIGATE CAUSE


- ELECTRICAL - ALTERNATOR OVERVOLTAGE
[] Diagnostic:...VOLTAGE WARNING LIGHT ILLUMINATED STEADY, ALTERNATOR FIELD CIRCUIT BREAKER TRIPPED
[] If Failed:...
[] Avionics Master...OFF
[] Master Switch ...OFF, THEN ON
[] Alternator Circuit Breaker...IF VOLTAGE WARNNG LIGHT STILL ILLUMINATED, RESET
[] Reduce Electrical Load ...TURN OFF ALL NON-ESSENTIAL ELECTRICAL EQUIPMENT
[] Land...AS SOON AS PRACTICAL AND INVESTIGATE CAUSE

- ELECTRICAL FIRE IN FLIGHT
[] Master Switch...OFF
[] Avionics Master...OFF
[] Cabin Ventilation...OPEN
[] Heating Controls...AS DESIRED
[] Circuit Breakers...CHECK
[] Master and Alternator Switches...ON, select ESSENTIAL switches one at a time; permit a short time to elapse before activating an additional circuit
[] Land...AS SOON AS PRACTICABLE AND INVESTIGATE CAUSE

- SPIN RECOVERY
[] Throttle...IDLE
[] Ailerons...NEUTRAL
[] Rudder...FULL OPPOSITE TO DIRECTION OF ROTATION
[] Yoke...FULL FORWARD
[] Rudder...NEUTRAL (when rotation stops)
[] Yoke...AS REQUIRED TO SMOOTHLY REGAIN LEVEL FLIGHT ATTITUDE
"
    };

    protected void Page_Load(object sender, EventArgs e)
    {
        string szCSSRef = String.Format(CultureInfo.InvariantCulture, "<link href=\"{0}?v=6\" type=\"text/css\" rel=\"stylesheet\">", ResolveClientUrl("~/Public/CSS/ChecklistBase.css"));
        Literal lit = new Literal();
        Header.Controls.Add(lit);
        lit.Text = szCSSRef;

        int sample = util.GetIntParam(Request, "s", -1);
        if (sample >= 0 && sample < rgSamples.Length)
        {
            pnlEditor.Visible = false;
            ChecklistItem1.DataItem = new Checklist(rgSamples[sample]).Root;
        }
    }

    protected void btnPreview_Click(object sender, EventArgs e)
    {
        ChecklistItem1.DataItem = new Checklist(txtChecklistSrc.Text).Root;
    }

    protected void btnLoadSample_Click(object sender, EventArgs e)
    {
        txtChecklistSrc.Text = rgSamples[0];
    }
}

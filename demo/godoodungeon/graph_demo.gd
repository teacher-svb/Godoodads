extends Node3D
## godoodungeon 1.1 graph demo: reads the dungeon graph that Generate stored on the GridMap, at
## runtime and without the plugin. It takes the two rooms farthest apart as start and goal, marks
## them and the shortest way between them, and labels every room with its distance from the start.
## Run the scene (F6). The dungeon is baked in; Generate in the editor makes a new one and its graph.

## The GridMap with the dungeon. Empty means the child called GridMap.
@export var grid_map: GridMap

const START := Color(0.2, 0.8, 0.3)
const GOAL := Color(0.9, 0.2, 0.2)
const PATH := Color(0.95, 0.75, 0.2)
const OTHER := Color(0.6, 0.6, 0.65)

var _markers: Node3D


func _ready() -> void:
	if grid_map == null:
		grid_map = $GridMap
	# Untyped on purpose: `var graph: DungeonGraph` works too, once the editor has scanned the project.
	var graph = grid_map.get_meta(&"godoodads_dungeon_graph", null)
	if graph == null:
		push_error("The GridMap has no dungeon graph. Generate the dungeon with godoodungeon 1.1 or later.")
		return

	# Room indices start at 0; Number is the room number the editor shows (index + 1).
	# A room whose Data says "boss" (from a room box, godoodungeon 1.2) is the goal, and the start is
	# the room farthest from it. Without one, the two rooms farthest apart are start and goal.
	var start := -1
	var goal := -1
	for room in graph.Rooms:
		if room.Data.get("role", "") == "boss":
			goal = room.Index
			start = graph.FarthestRoom(goal)
	if goal < 0:
		var ends: Vector2i = graph.Diameter()
		start = ends.x
		goal = ends.y
	var path: PackedInt32Array = graph.ShortestPath(start, goal)
	var path_corridors: PackedInt32Array = graph.ShortestPathCorridors(start, goal)
	var hops: PackedInt32Array = graph.HopDistances(start)
	var lengths: PackedInt32Array = graph.Distances(start)

	_markers = Node3D.new()
	_markers.name = "GraphMarkers"
	add_child(_markers)

	# Data is free for gameplay: here it records the roles this script chose.
	graph.Rooms[start].Data["role"] = "start"
	if graph.Rooms[goal].Data.get("role", "") != "boss":
		graph.Rooms[goal].Data["role"] = "goal"

	for room in graph.Rooms:
		var colour := OTHER
		if room.Index == start:
			colour = START
		elif room.Index == goal:
			colour = GOAL
		elif room.Index in path:
			colour = PATH
		var text := "Room %d%s\n%d hops · %d cells" % [room.Number, " (boss)" if room.Data.get("role", "") == "boss" else "",
			hops[room.Index], lengths[room.Index]]
		_mark(graph.RoomCentre(grid_map, room.Index), colour, 0.9, text)

	for corridor in path_corridors:
		for cell in graph.Corridors[corridor].Cells:
			_mark(grid_map.to_global(grid_map.map_to_local(cell)), PATH, 0.4, "")

	var numbers := PackedStringArray()
	for room in path:
		numbers.append(str(graph.Rooms[room].Number))
	print("Dungeon graph: %d rooms, %d corridors. Start room %d, goal room %d: %d corridors and %d corridor cells apart, through rooms %s." % [
		graph.Rooms.size(), graph.Corridors.size(), graph.Rooms[start].Number, graph.Rooms[goal].Number,
		hops[goal], lengths[goal], ", ".join(numbers)])


## A coloured disc at a position, with an optional label next to it that faces the camera.
func _mark(at: Vector3, colour: Color, radius: float, text: String) -> void:
	var material := StandardMaterial3D.new()
	material.albedo_color = colour
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	var disc := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = 0.1
	mesh.material = material
	disc.mesh = mesh
	_markers.add_child(disc)
	disc.global_position = at + Vector3.UP * 0.6
	if text.is_empty():
		return
	var label := Label3D.new()
	label.text = text
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label.no_depth_test = true
	label.font_size = 48
	label.outline_size = 16
	label.pixel_size = 0.03
	label.modulate = colour.lightened(0.4)
	disc.add_child(label)
	label.position = Vector3(0, 1.5, 2.4)  # below the disc as seen from above

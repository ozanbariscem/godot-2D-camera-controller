extends Camera2D


onready var new_zoom:Vector2 = zoom
onready var new_position:Vector2 = position

var ignore_input:bool = false

onready var last_mouse_position:Vector2 = get_viewport().get_mouse_position()

export var mouse_movement_speed:float = 3500
export var mouse_zoom_speed:float = 15
export var mouse_movement_deadzone_size:float = 0.3

export var keyboard_movement_speed:float = 2500
export var keyboard_zoom_speed:float = 5

export var border_movement_speed:float = 2500
export var border_movement_range:float = 0.05

export var smooth_move_speed:float = 50
export var smooth_zoom_speed:float = 5


export var zoom_limit:Vector2 = Vector2(.5, 2)
export var horizontal_movement_limit:Vector2 = Vector2(-900, 900)
export var vertical_movement_limit:Vector2 = Vector2(-900, 900)

export var curve:Curve
func get_curve_multiplier() -> float:
	if slow_camera_over_distance:
		return curve.interpolate(self.zoom.x/zoom_limit.y)
	return 1.0


export var limit_horizontal_movement:bool = true
export var limit_vertical_movement:bool = true
export var limit_zoom:bool = true

export var keyboard_movement:bool = true
export var keyboard_zoom:bool = true

export var mouse_movement:bool = true
export var mouse_zoom:bool = true

export var border_movement:bool = true

export var smooth_zoom:bool = true
export var smooth_move:bool = true

export var slow_camera_over_distance:bool = true
export var zoom_to_mouse:bool = true
export var keyboard_zoom_also_zooms_to_mouse:bool = false

onready var viewport:Viewport = get_viewport()
onready var rect:Rect2 = get_viewport_rect()


func _ready():
	current = true


func _process(delta):
	pass


func _physics_process(delta):
	if keyboard_zoom:
		_keyboard_zoom_input(delta)
	if keyboard_movement:
		_keyboard_movement_input(delta)
	if mouse_zoom:
		_mouse_zoom_input(delta)
	if mouse_movement:
		_mouse_movement_input(delta)
	if border_movement:
		_border_movement_input(delta)
	
	_zoom(delta)
	_move(delta)


func _move(delta):
	if limit_horizontal_movement or limit_vertical_movement:
		var x = new_position.x
		var y = new_position.y
		
		if limit_horizontal_movement:
			x = clamp(new_position.x, horizontal_movement_limit.x, horizontal_movement_limit.y)
		if limit_vertical_movement:
			y = clamp(new_position.y, vertical_movement_limit.x, vertical_movement_limit.y)
		
		new_position = Vector2(x, y)
	
	if smooth_move:
		self.position = self.position.linear_interpolate(new_position, smooth_move_speed * delta)
	else:
		self.position = new_position


func _zoom(delta):
	if limit_zoom:
		var limit = clamp(new_zoom.x, zoom_limit.x, zoom_limit.y)
		new_zoom = Vector2(limit, limit)
	
	if smooth_zoom:
		self.zoom = self.zoom.linear_interpolate(new_zoom, smooth_zoom_speed * delta)
	else:
		self.zoom = new_zoom


func _keyboard_zoom_input(delta):
	if Input.is_action_pressed("zoom_in"):
		_zoom_input(-1, keyboard_zoom_speed, delta)
		
		if keyboard_zoom_also_zooms_to_mouse and zoom_to_mouse and zoom.x > zoom_limit.x + 0.01:
			var mouse_position:Vector2 = get_global_mouse_position()
			var distance = mouse_position - self.position
			_move_input(distance.normalized().round(), keyboard_movement_speed, delta)
	if Input.is_action_pressed("zoom_out"):
		_zoom_input(1, keyboard_zoom_speed, delta)


func _keyboard_movement_input(delta):
	if Input.is_action_pressed('move_up'):
		_move_input(Vector2(0, -1), keyboard_movement_speed, delta)
	if Input.is_action_pressed('move_down'):
		_move_input(Vector2(0, 1), keyboard_movement_speed, delta)
	if Input.is_action_pressed('move_left'):
		_move_input(Vector2(-1, 0), keyboard_movement_speed, delta)
	if Input.is_action_pressed('move_right'):
		_move_input(Vector2(1, 0), keyboard_movement_speed, delta)


func _mouse_zoom_input(delta):
	if Input.is_action_just_released('zoom_in'):
		_zoom_input(-1, mouse_zoom_speed, delta)
		
		if zoom_to_mouse and zoom.x > zoom_limit.x + 0.01:
			var mouse_position:Vector2 = get_global_mouse_position()
			var distance = mouse_position - self.position
			_move_input(distance.normalized().round(), mouse_movement_speed, delta)
	if Input.is_action_just_released('zoom_out'):
		_zoom_input(1, mouse_zoom_speed, delta)


func _mouse_movement_input(delta):
	if Input.is_action_just_pressed("mouse_move_trigger"):
		last_mouse_position = viewport.get_mouse_position()
	if Input.is_action_pressed("mouse_move_trigger"):
		var mouse_position = viewport.get_mouse_position()
		var direction:Vector2 = last_mouse_position - mouse_position
		last_mouse_position = mouse_position
		
		direction = direction.normalized()
		
		if direction.x >= mouse_movement_deadzone_size:
			_move_input(Vector2(1, 0), mouse_movement_speed, delta)
		elif direction.x <= -mouse_movement_deadzone_size:
			_move_input(Vector2(-1, 0), mouse_movement_speed, delta)
		if direction.y >= mouse_movement_deadzone_size:
			_move_input(Vector2(0, 1), mouse_movement_speed, delta)
		elif direction.y <= -mouse_movement_deadzone_size:
			_move_input(Vector2(0, -1), mouse_movement_speed, delta)


func _border_movement_input(delta):
	var mouse_position:Vector2 = viewport.get_mouse_position()
	
	if mouse_position.x < border_movement_range * rect.size.x:
		_move_input(Vector2(-1, 0), border_movement_speed, delta)
	elif mouse_position.x > (1 - border_movement_range) * rect.size.x:
		_move_input(Vector2(1, 0), border_movement_speed, delta)
	if mouse_position.y < border_movement_range * rect.size.y:
		_move_input(Vector2(0, -1), border_movement_speed, delta)
	elif mouse_position.y > (1 - border_movement_range) * rect.size.y:
		_move_input(Vector2(0, 1), border_movement_speed, delta)


func _zoom_input(var value:int, var multiplier:float, var delta:float):
	new_zoom += Vector2(value, value) * multiplier * delta * get_curve_multiplier()


func _move_input(var value:Vector2, var multiplier:float, var delta:float):
	new_position += value * multiplier * delta * get_curve_multiplier()

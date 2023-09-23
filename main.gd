extends Node3D

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	DebugDraw2D.set_text("FPS", Engine.get_frames_per_second())

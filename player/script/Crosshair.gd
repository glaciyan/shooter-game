extends TextureRect


# Called when the node enters the scene tree for the first time.
func _ready():
	get_viewport().size_changed.connect(adjust_size_to_viewport)
	adjust_size_to_viewport()

func adjust_size_to_viewport():
	set_deferred("size", get_viewport().get_visible_rect().size)

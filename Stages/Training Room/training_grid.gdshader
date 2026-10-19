// Training Room grid surface.
// World-space grid used on the floor, back wall and side walls of the training stage.
// Every value is a uniform so the look can be tuned per material in the Inspector.
shader_type spatial;
render_mode blend_mix, cull_back, diffuse_burley, specular_schlick_ggx;

group_uniforms Surface;
uniform vec3 base_color : source_color = vec3(0.60, 0.60, 0.58);
uniform float floor_brightness : hint_range(0.0, 2.0) = 0.92;
uniform float tile_variation : hint_range(0.0, 0.2) = 0.035;
uniform float grain_strength : hint_range(0.0, 0.3) = 0.05;
uniform float grain_scale = 24.0;
uniform float surface_roughness : hint_range(0.0, 1.0) = 0.85;

group_uniforms Grid;
uniform float cell_size = 0.5;
uniform int cells_per_major = 2;
uniform vec3 minor_line_color : source_color = vec3(0.30, 0.30, 0.30);
uniform vec3 major_line_color : source_color = vec3(0.13, 0.13, 0.14);
uniform vec3 highlight_line_color : source_color = vec3(0.92, 0.92, 0.92);
uniform float minor_line_width = 0.012;
uniform float major_line_width = 0.028;
uniform float highlight_line_width = 0.014;
uniform float highlight_line_offset = 0.04;

group_uniforms Markers;
uniform vec3 center_line_color : source_color = vec3(0.08, 0.18, 0.62);
uniform float center_line_width = 0.05;
uniform vec3 band_color : source_color = vec3(0.45, 0.68, 0.38);
uniform float band_width = 0.55;
uniform float band_strength : hint_range(0.0, 1.0) = 0.85;
// Depth (world Z) on the side walls where the fighting plane meets the corner.
uniform float side_marker_z = 0.0;
// Height of the gameplay ceiling (GlobalVariables.CeilingLimit / SimulationScale).
uniform float ceiling_height = 12.0;
uniform vec3 ceiling_line_color : source_color = vec3(0.75, 0.12, 0.10);
uniform float ceiling_line_width = 0.06;

group_uniforms Shading;
// Fake contact shadow along every inside corner (the project uses the
// Compatibility renderer, which has no SSAO).
uniform float corner_shadow_strength : hint_range(0.0, 1.0) = 0.35;
uniform float corner_shadow_distance = 1.2;
uniform float wall_limit = 7.5;
uniform float back_wall_z = -5.0;

varying vec3 world_pos;
varying vec3 world_normal;

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	float a = hash12(i);
	float b = hash12(i + vec2(1.0, 0.0));
	float c = hash12(i + vec2(0.0, 1.0));
	float d = hash12(i + vec2(1.0, 1.0));
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Anti-aliased line mask. coord is in world units, aa is its screen-space derivative.
float line_mask(float coord, float spacing, float width, float aa) {
	float d = abs(fract(coord / spacing + 0.5) - 0.5) * spacing;
	return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, d);
}

float single_line(float coord, float position, float width, float aa) {
	float d = abs(coord - position);
	return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, d);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec3 n = abs(world_normal);
	float is_floor = step(0.5, n.y);
	float is_side = (1.0 - is_floor) * step(n.z, n.x);
	float is_back = (1.0 - is_floor) * (1.0 - is_side);

	// 2D surface coordinates: U runs across the surface, V runs up it (or into the stage on the floor).
	vec2 uv = world_pos.xz * is_floor + world_pos.xy * is_back + world_pos.zy * is_side;
	vec2 aa = fwidth(uv);

	float major_spacing = cell_size * float(max(cells_per_major, 1));

	// Base surface: per-tile brightness variation plus fine grain.
	vec2 tile_id = floor(uv / cell_size);
	float tile = (hash12(tile_id) - 0.5) * 2.0 * tile_variation;
	float grain = (value_noise(uv * grain_scale) - 0.5) * grain_strength;
	grain += (value_noise(uv * grain_scale * 0.23 + 17.0) - 0.5) * grain_strength * 0.3;
	vec3 color = base_color * (1.0 + tile + grain);
	color *= mix(1.0, floor_brightness, is_floor);

	// Center band and line run along world X = 0 (floor and back wall only).
	float on_center_axis = is_floor + is_back;
	float band = single_line(world_pos.x, 0.0, band_width, aa.x) * on_center_axis;
	// Matching band on each side wall where the fighting plane hits the corner.
	band = max(band, single_line(world_pos.z, side_marker_z, band_width, aa.x) * is_side);
	color = mix(color, band_color * (1.0 + grain), band * band_strength);

	// Grid lines.
	float minor = max(line_mask(uv.x, cell_size, minor_line_width, aa.x), line_mask(uv.y, cell_size, minor_line_width, aa.y));
	float major = max(line_mask(uv.x, major_spacing, major_line_width, aa.x), line_mask(uv.y, major_spacing, major_line_width, aa.y));
	float highlight = max(
		line_mask(uv.x - highlight_line_offset, cell_size, highlight_line_width, aa.x),
		line_mask(uv.y - highlight_line_offset, cell_size, highlight_line_width, aa.y));
	color = mix(color, highlight_line_color, highlight * 0.85);
	color = mix(color, minor_line_color, minor);
	color = mix(color, major_line_color, major);

	// Blue center line on floor and back wall, and at the fighting plane on side walls.
	float center = single_line(world_pos.x, 0.0, center_line_width, aa.x) * on_center_axis;
	center = max(center, single_line(world_pos.z, side_marker_z, center_line_width, aa.x) * is_side);
	color = mix(color, center_line_color, center);

	// Ceiling limit line on all walls.
	float ceiling = single_line(world_pos.y, ceiling_height, ceiling_line_width, aa.y) * (1.0 - is_floor);
	color = mix(color, ceiling_line_color, ceiling);

	// Contact shadow into the inside corners.
	float d_floor = mix(world_pos.y, 1e4, is_floor);
	float d_side = wall_limit - abs(world_pos.x);
	d_side = mix(d_side, 1e4, is_side);
	float d_back = mix(world_pos.z - back_wall_z, 1e4, is_back);
	float d = min(d_floor, min(d_side, d_back));
	float occlusion = 1.0 - corner_shadow_strength * (1.0 - smoothstep(0.0, corner_shadow_distance, d));

	ALBEDO = color * occlusion;
	ROUGHNESS = surface_roughness;
	SPECULAR = 0.25;
}

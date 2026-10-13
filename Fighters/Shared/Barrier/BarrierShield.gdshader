// Barrier shield: a glowing magic circle drawn procedurally (no textures).
// Colours and speed are uniforms, so they can be tuned on the material in BarrierShield.tscn.
shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;

uniform vec4 tint : source_color = vec4(0.35, 1.0, 0.8, 1.0);
uniform vec4 core_tint : source_color = vec4(0.75, 1.0, 1.0, 1.0);
uniform float strength = 1.2;
uniform float spin_speed = 0.6;

float ring(float r, float radius, float width) {
	return smoothstep(width, 0.0, abs(r - radius));
}

float line_dist(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a, ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h);
}

float triangle_lines(vec2 p, float radius, float rot, float width) {
	float d = 10.0;
	for (int i = 0; i < 3; i++) {
		float a0 = rot + float(i) * 2.0943951;
		float a1 = rot + float(i + 1) * 2.0943951;
		vec2 v0 = vec2(cos(a0), sin(a0)) * radius;
		vec2 v1 = vec2(cos(a1), sin(a1)) * radius;
		d = min(d, line_dist(p, v0, v1));
	}
	return smoothstep(width, 0.0, d);
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float r = length(p);
	if (r > 1.0) discard;
	float ang = atan(p.y, p.x);
	float t = TIME * spin_speed;

	// rings
	float g = ring(r, 0.95, 0.025) + ring(r, 0.86, 0.012) + ring(r, 0.62, 0.012) + ring(r, 0.30, 0.01);

	// rotating rune band between the two outer rings
	float seg = (ang + t) / 6.2831853 * 36.0;
	float cell = floor(seg);
	float rnd = fract(sin(cell * 91.345) * 47453.21);
	float in_band = step(0.875, r) * step(r, 0.935);
	float rune = in_band * step(0.25, fract(seg)) * step(fract(seg), 0.75) * step(0.35, rnd);

	// tick marks on the inner band, turning the other way
	float seg2 = (ang - t * 1.5) / 6.2831853 * 72.0;
	float ticks = step(0.64, r) * step(r, 0.84) * smoothstep(0.12, 0.0, abs(fract(seg2) - 0.5)) * 0.6;

	// hexagram in the middle
	float hex = triangle_lines(p, 0.6, t * 0.5, 0.012) + triangle_lines(p, 0.6, t * 0.5 + 3.14159265, 0.012);

	// soft fill and bright edge
	float fill = smoothstep(1.0, 0.2, r) * 0.12;
	float edge = smoothstep(0.8, 1.0, r) * 0.25;

	float pulse = 0.85 + 0.15 * sin(TIME * 9.0);
	float lines = clamp(g + rune + ticks + hex * 0.9, 0.0, 1.0);
	float glow = fill + edge + smoothstep(0.35, 0.0, r) * 0.1;
	ALBEDO = mix(tint.rgb, core_tint.rgb, lines * 0.5) * strength;
	ALPHA = clamp((lines + glow) * pulse, 0.0, 1.0);
}

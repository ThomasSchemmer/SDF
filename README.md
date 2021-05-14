# SDF
An ongoing experiment with different rendering techniques

Started with a raycaster as vertex shader to display SDF's, but now includes:
1) SDF Ray*marcher* in compute shader, with movable camera
2) Marching Cubes in compute shader *doesn't really work, but used to in c# script*
3) Grass shader, using
  * deferred lighting for custom shadow passes
  * Procedural drawing, directly hooked into pipeline
  * ComputeShader to dynamically update grass
  * perlin noise texture to represent wind

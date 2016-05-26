using UnityEngine;
using System.Collections.Generic;
using Leap;
using System;

public class CanvasAutomation : MonoBehaviour {

	public Material material_;

	private const int INTERPOLATION_AMT = 3;
	private const int MAX_POINTS = 500; //make it infinite
	private const int POINT_MEMORY = 50;
	private const int TUBE_FACES = 50;
	private const int VERTEX_NUM = TUBE_FACES * 2;
	private const int TRIANGLE_NUM = TUBE_FACES * 6;
	private const float MULTI_HAND_PHASE = 0.1f;

	//size of the brush and the multiplier for a more direct size change
	int size = 50;
	float multiplier_size = 1f;

	private bool FIST = false;

	private Mesh mesh_;

	private int point_index_ = 0;

	private float line_width_ = 0.6f;
	private float current_line_width_ = 0.0f;
	private float multi_hand_phase_ = 0.0f;

	private Vector3 last_velocity_;
	private Vector3 last_drawn_point_;
	private Vector3[] drawn_points_;
	private Vector3[] last_points_;
	private Vector3[] last_shape_;

	private bool drawing_;
	private List<Vector3> next_points_;

	void Start () {
		mesh_ = new Mesh();
	    drawing_ = false;

	    next_points_ = new List<Vector3>();
	    drawn_points_ = new Vector3[MAX_POINTS];
	    last_points_ = new Vector3[POINT_MEMORY];
	    last_shape_ = null;
	}

	//called from the drawer.cs file, used to draw when pinching
	public void AddNextPoint(Vector3 next, bool fist) {   //get the input for fist as well
		if (!drawing_) {
			for (int i = 0; i < POINT_MEMORY; ++i)
				last_points_[i] = next;
			last_drawn_point_ = next;
			last_velocity_ = Vector3.zero;
		}

		if (fist)
			multiplier_size = 2f;
		else
			multiplier_size = 0.5f;

		FIST = fist;

		next_points_.Add(next);
		drawing_ = true;
	}

	//lateupdate used instead of update
	void LateUpdate() {
		drawing_ = next_points_.Count != 0;

		UpdateNextPoint();
		Graphics.DrawMesh(mesh_, transform.localToWorldMatrix, material_, 0);
	}

	private void UpdateNextPoint() {
		if (drawing_) {
			for (int i = POINT_MEMORY - 1; i > 0; i--)
				last_points_[i] = last_points_[i - 1];

			Vector3 point1 = next_points_[0];
			Vector3 point2 = next_points_[1 % next_points_.Count];
			float t = Mathf.Sin(multi_hand_phase_) / 2.0f + 0.5f;
			last_points_[0] = point1 + t * (point2 - point1);
			multi_hand_phase_ += MULTI_HAND_PHASE;
			next_points_.Clear();
		}

		for (int i = 1; i <= INTERPOLATION_AMT; ++i) {
			IncrementIndices ();

			Vector3 draw_point = InterpolatedPoint((1.0f * i) / INTERPOLATION_AMT);
			last_velocity_ = last_velocity_ + 0.4f * (draw_point - last_drawn_point_ - last_velocity_);

			if (drawing_) {
				last_shape_ = MakeShape(draw_point);
				AddLine(mesh_, last_shape_);
				drawn_points_[point_index_] = draw_point;
			}
			else {
				last_shape_ = null;
				current_line_width_ = 0.0f;
			}
			last_drawn_point_ = draw_point;
		}
	}

	void IncrementIndices() {
		point_index_ = (point_index_ + 1) % MAX_POINTS;
	}
  
	private Vector3 InterpolatedPoint(float t) {
		Vector3 naut = last_points_[0] * (t + 2) * (t + 1) * t / 6.0f;
		Vector3 one = -last_points_[1] * (t + 2) * (t + 1) * (t - 1) / 2.0f;
		Vector3 two = last_points_[2] * (t + 2) * t * (t - 1) / 2.0f;
		Vector3 three = -last_points_[3] * (t + 1) * t * (t - 1) / 6.0f;
		return naut + one + two + three;
	}

	Vector3[] MakeShape(Vector3 next) {
		Vector3[] q = new Vector3[VERTEX_NUM];
		Vector3 radius = new Vector3();

		//alternative variable to TUBE_FACES (constant), for the amount of sides of the tube
		float t = TUBE_FACES;

		//change radius according to speed, size is proportional to velocity with which width is computed
		if (!FIST) {
			float compute_width = (1.0f / (size * last_velocity_.magnitude + 1.0f)) * line_width_ * multiplier_size;
			current_line_width_ = current_line_width_ + 0.1f * (compute_width - current_line_width_);
			radius = new Vector3 (0.0f, current_line_width_ / 2.0f, 0.0f);
		} else {
			//the width resize with the fist is affected by 1/5 of the velocity instead of 1/1 of normal drawing
			float compute_width = (1.0f / (size * last_velocity_.magnitude + 1.0f)) * line_width_ * multiplier_size;
			current_line_width_ = current_line_width_ + 0.1f * (compute_width - current_line_width_);
			radius = new Vector3 (0.0f, current_line_width_ / 2.0f, 0.0f);
			t = 4;
		}
			
		//change the amount of faces according to speed
		//does not really affect visuals
		/*float t = TUBE_FACES*last_velocity_.magnitude*5;
		int sides = 3;
		float d = 10 / last_velocity_.magnitude;
		if (d > 100 && d <= 200) {
			sides = 3;
		} else if (d > 200 && d <= 300) {
			sides = 13;
		} else if (d > 300 && d <= 400) {
			sides = 23;
		} else if (d > 400 && d <= 500) {
			sides = 33;
		} else if (d > 500 && d <= 600) {
			sides = 43;
		} else {
			sides = 50;
		}*/


		Quaternion rotation = Quaternion.AngleAxis(360.0f / t, new Vector3(0, 0, 1));

		if (last_shape_ == null) {
			for (int i = 0; i < TUBE_FACES; ++i)
				q[i] = transform.InverseTransformPoint(last_drawn_point_);
		}
		else {
			for (int i = 0; i < TUBE_FACES; ++i)
				q[i] = transform.InverseTransformPoint(last_shape_[TUBE_FACES + i]);
		}

		for (int i = 0; i < TUBE_FACES; ++i) {
			q[TUBE_FACES + i] = transform.InverseTransformPoint(next + radius);
			radius = rotation * radius;
		}

		return q;
	}

	void AddLine(Mesh m, Vector3[] shape) {
		int vertex_index = point_index_ * VERTEX_NUM;
		int triangle_index = point_index_ * TRIANGLE_NUM;
		// Update Vertices.
		Vector3[] vertices = m.vertices;
		if (vertex_index >= m.vertices.Length) {
			Array.Resize(ref vertices, MAX_POINTS * VERTEX_NUM);
		}

		for (int i = 0; i < VERTEX_NUM; ++i) {
			vertices[vertex_index + i] = shape[i];
			float t = (1.0f * vertex_index + i) / (MAX_POINTS * VERTEX_NUM);
		}

		// Update triangles.
		int[] triangles = m.triangles;
		if (triangle_index >= m.triangles.Length)
			Array.Resize(ref triangles, MAX_POINTS * TRIANGLE_NUM);

		for (int i = 0; i < TUBE_FACES; ++i) {
			int t_index = triangle_index + i * 6;
			triangles[t_index] = vertex_index + i;
			triangles[t_index + 1] = vertex_index + i + TUBE_FACES;
			triangles[t_index + 2] = vertex_index + (i + 1) % TUBE_FACES;
			triangles[t_index + 3] = vertex_index + TUBE_FACES + (i + 1) % TUBE_FACES;
			triangles[t_index + 4] = vertex_index + (i + 1) % TUBE_FACES;
			triangles[t_index + 5] = vertex_index + i + TUBE_FACES;
		}

		m.vertices = vertices;
		m.triangles = triangles;
		m.RecalculateBounds();
	}
		
	public Vector3 GetLastPoint() {
		return drawn_points_[point_index_];
	}
}

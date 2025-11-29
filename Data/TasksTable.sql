-- Tasks table schema for moving from in-memory List<TaskItem> to real DB storage.
-- Target DB: (e.g. PostgreSQL)

CREATE TABLE Tasks (
	id SERIAL PRIMARY KEY,
	title VARCHAR(255) NOT NULL,
	description TEXT,
	is_completed BOOLEAN NOT NULL DEFAULT FALSE,
	created_at TIMESTAMP NOT NULL,
	updated_at TIMESTAMP NOT NULL
);
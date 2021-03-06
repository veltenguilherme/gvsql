CREATE TABLE public.{0}(uuid uuid PRIMARY KEY NOT NULL DEFAULT uuid_generate_v4(),{1});
ALTER TABLE public.{0} OWNER to postgres;

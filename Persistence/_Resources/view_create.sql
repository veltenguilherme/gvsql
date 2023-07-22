CREATE OR REPLACE VIEW public.{0}({1})
AS
{2}
order by {3}.updated desc nulls last, {3}.inserted desc;
		 
ALTER TABLE public.{0} OWNER TO postgres;
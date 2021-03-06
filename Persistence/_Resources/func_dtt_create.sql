CREATE OR REPLACE FUNCTION {0}() 
RETURNS TRIGGER AS $$
BEGIN
    NEW.{1} = now();
    RETURN NEW; 
END;
$$ language 'plpgsql';
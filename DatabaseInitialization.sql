create user rebus_test password 'Password01';
create database rebus_test;
GRANT ALL PRIVILEGES ON DATABASE rebus_test TO rebus_test;

-- run on DB rebus_test as superadmin
GRANT ALL PRIVILEGES ON SCHEMA public TO rebus_test;

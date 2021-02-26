-- Companion Cube database schema
-- for PostgreSQL 10+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         8 to 9
-- Last update:     2021-02-26 09:57 +01:00
--
-- ------------------------------------------------------------------------
-- 
-- This file is part of Companion Cube project
--
-- Copyright 2020 Emzi0767
-- 
-- Licensed under the Apache License, Version 2.0 (the "License");
-- you may not use this file except in compliance with the License.
-- You may obtain a copy of the License at
-- 
--   http://www.apache.org/licenses/LICENSE-2.0
-- 
-- Unless required by applicable law or agreed to in writing, software
-- distributed under the License is distributed on an "AS IS" BASIS,
-- WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
-- See the License for the specific language governing permissions and
-- limitations under the License.
-- 
-- ------------------------------------------------------------------------
-- 
-- Tables, migrations, and conversions

-- Update schema version in the database
update metadata set meta_value = '9' where meta_key = 'schema_version';
update metadata set meta_value = '2021-02-26T09:57+01:00' where meta_key = 'timestamp';

-- ------------------------------------------------------------------------

-- create trigram index on tags
create index ix_tags_name_trgm on tags using gin(name gin_trgm_ops);
